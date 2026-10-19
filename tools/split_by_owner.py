#!/usr/bin/env python3
"""
Reparte el trabajo de una rama fuente en una rama por integrante (dueño), para abrir un PR por persona.

Uso:
    python tools/split_by_owner.py --base origin/main --source main \
        --branch jose=jose/base-juego --branch majo=majo/docs-ui-datos --branch daniel=daniel/core-pruebas

Qué hace:
  1. Si la fuente comparte historia con la base, recorre los commits base..fuente en orden cronológico
     (primer padre). Si no, toma el diff total y lo reparte por carpetas en orden lógico
     (datos, núcleo, UI, escenas, docs).
  2. Por cada dueño crea (o rehace) su rama desde la base y aplica SOLO sus archivos, en commits pequeños:
     uno por grupo lógico (carpeta) de cada commit original.
  3. Cada commit va firmado por el dueño y conserva la fecha original (minutos de diferencia entre partes).
  4. Verifica que fusionar las ramas da un árbol idéntico a la fuente.

No toca la carpeta de trabajo ni el índice: usa un índice temporal por dueño y comandos de bajo nivel de git.
El resultado es determinista: con la misma base y los mismos commits produce los mismos hashes. Por eso,
si la fuente crece (más commits encima), las ramas nuevas continúan exactamente a las anteriores.
"""
from __future__ import annotations

import argparse
import os
import re
import subprocess
import sys
import tempfile
from dataclasses import dataclass, field
from datetime import datetime, timedelta

# ---------------------------------------------------------------------------------------------
# Configuración del equipo
# ---------------------------------------------------------------------------------------------

AUTHORS = {
    "jose": None,  # se lee de git config user.name / user.email
    "majo": ("maajjjoo", "majose2006@gmail.com"),
    "daniel": ("Daniel-I24", "ivan242005d@gmail.com"),
}

P = "Assets/_Project/"

# Reglas de dueño: la primera que coincide gana. Si ninguna coincide, el archivo es de JOSE.
# Cada entrada es un prefijo de carpeta (coincide la carpeta y todo lo de adentro) o una expresión regular.
OWNER_RULES: list[tuple[str, str]] = [
    ("majo", "Docs"),
    ("majo", P + "Scripts/UI"),
    ("majo", P + "UI"),
    ("majo", P + "Data"),
    ("majo", "LEEME-PRIMERO.md"),
    # Builders de datos de los casos y de los assets compartidos (UITheme, AiSettings, canales): generan Data/ y UI/.
    ("majo", r"re:^Assets/_Project/Editor/(Case\d+DataBuilder|SharedAssetsBuilder)\.cs$"),
    ("daniel", P + "Scripts/Core"),
    ("daniel", P + "Scripts/Save"),
    ("daniel", P + "Scripts/Evidence"),
    ("daniel", P + "Scripts/Decisions"),
    ("daniel", P + "Scripts/Dialogue"),
    ("daniel", P + "Scripts/AI"),
    ("daniel", P + "Tests"),
    ("daniel", "Server"),
]

# Orden lógico de las áreas dentro de un commit (datos, núcleo, UI, escenas, pruebas, docs).
AREA_ORDER = ["datos", "core", "ia", "jugador", "interaccion", "config", "editor", "ui", "arte",
              "escenas", "pruebas", "server", "herramientas", "docs"]


def strip_meta(path: str) -> str:
    """Los .meta van con su archivo (o con su carpeta)."""
    return path[:-5] if path.endswith(".meta") else path


def owner_of(path: str) -> str:
    p = strip_meta(path)
    for owner, rule in OWNER_RULES:
        if rule.startswith("re:"):
            if re.match(rule[3:], p):
                return owner
        elif p == rule or p.startswith(rule + "/"):
            return owner
    return "jose"


def group_of(path: str) -> tuple[str, str]:
    """Devuelve (área, clave de grupo). Archivos con la misma clave en un commit van juntos."""
    p = strip_meta(path)
    parts = p.split("/")
    if p.startswith(P):
        rest = parts[2:]
        top = rest[0] if rest else ""
        sub = rest[1] if len(rest) > 2 or (len(rest) == 2 and "." not in rest[1]) else ""
        key = P + top + ("/" + sub if sub else "")
        if top == "Data":
            return "datos", key
        if top == "UI":
            return "ui", key
        if top == "Scripts":
            area = {"UI": "ui", "Core": "core", "Save": "core", "Evidence": "core", "Decisions": "core",
                    "Dialogue": "core", "AI": "ia", "Player": "jugador", "Interaction": "interaccion"}.get(sub, "config")
            if area == "core" and len(rest) > 2 and rest[2] == "Data":
                area, key = "datos", key + "/Data"
            return area, key
        if top == "Editor":
            name = rest[-1]
            if re.match(r"(Case\d+DataBuilder|SharedAssetsBuilder)\.cs$", name):
                return "datos", P + "Editor/datos"
            if name.endswith("SceneBuilder.cs") or name in ("SceneKit.cs", "ScenePreviewCapture.cs"):
                return "escenas", P + "Editor/escenas"
            return "editor", P + "Editor"
        if top == "Tests":
            return "pruebas", key
        if top == "Scenes":
            return "escenas", key
        if top in ("Art", "Prefabs", "Audio"):
            return "arte", key
        return "config", key
    if parts[0] == "Docs":
        return "docs", "/".join(parts[:2]) if len(parts) > 2 else "Docs"
    if parts[0] == "Server":
        return "server", "Server"
    if parts[0] == "tools":
        return "herramientas", "tools"
    if parts[0] in ("ProjectSettings", "Packages"):
        return "config", parts[0]
    if parts[0] == ".claude":
        return "config", ".claude"
    if parts[0] == "Assets":
        if len(parts) > 2 and parts[1] in ("Settings", "Scenes"):
            return "config", "Assets/" + parts[1]
        return "config", "Assets/" + (parts[1] if len(parts) > 1 else "")
    if p.endswith(".md"):
        return "docs", p
    return "config", "raiz"


def commit_type(area: str, statuses: set[str], subject: str) -> str:
    if area == "pruebas":
        return "test"
    if area == "docs":
        return "docs"
    if area == "arte":
        return "art"
    if area in ("config", "herramientas"):
        return "chore"
    low = subject.lower()
    if re.search(r"\b(corrige|arregla|fix|error)\b", low):
        return "fix"
    if statuses == {"M"} and re.search(r"\b(refactor|reorganiza|limpieza)\b", low):
        return "refactor"
    return "feat"


AREA_LABEL = {"datos": "datos", "core": "core", "ia": "ia", "jugador": "jugador", "interaccion": "interaccion",
              "config": "config", "editor": "editor", "ui": "ui", "arte": "arte", "escenas": "escenas",
              "pruebas": "pruebas", "server": "server", "herramientas": "herramientas", "docs": "docs"}

# ---------------------------------------------------------------------------------------------
# Git
# ---------------------------------------------------------------------------------------------


def git(*args: str, env: dict | None = None, input_bytes: bytes | None = None, check: bool = True) -> str:
    full_env = os.environ.copy()
    if env:
        full_env.update(env)
    r = subprocess.run(["git", *args], capture_output=True, env=full_env, input=input_bytes)
    if check and r.returncode != 0:
        sys.exit(f"git {' '.join(args)} falló:\n{r.stderr.decode('utf-8', 'replace')}")
    return r.stdout.decode("utf-8", "replace")


@dataclass
class Change:
    status: str  # A, M, D, T
    path: str
    mode: str
    sha: str


@dataclass
class Part:
    owner: str
    area: str
    key: str
    changes: list[Change] = field(default_factory=list)


def diff_tree(old: str, new: str) -> list[Change]:
    out = git("diff-tree", "-r", "-z", "--no-renames", "--raw", old, new)
    tokens = out.split("\0")
    changes = []
    i = 0
    while i < len(tokens) - 1:
        meta = tokens[i]
        path = tokens[i + 1]
        i += 2
        if not meta.startswith(":"):
            continue
        _old_mode, new_mode, _old_sha, new_sha, status = meta[1:].split(" ")
        changes.append(Change(status[0], path, new_mode, new_sha))
    return changes


def short_names(changes: list[Change], limit: int = 3) -> str:
    names = []
    for c in changes:
        n = strip_meta(c.path).rsplit("/", 1)[-1]
        n = re.sub(r"\.(cs|asset|unity|md|mjs|json|asmdef|mat|fbx|png)$", "", n)
        if n not in names:
            names.append(n)
    shown = ", ".join(names[:limit])
    return shown + (f" y {len(names) - limit} más" if len(names) > limit else "")


def verb_for(changes: list[Change]) -> str:
    st = {c.status for c in changes}
    if st == {"A"}:
        return "agrega"
    if st == {"D"}:
        return "elimina"
    return "actualiza"


def clean_subject(subject: str) -> str:
    s = re.sub(r"^(Fase [A-Z]( \(\d/\d\))?|Caso \d|tipo\([^)]*\)):\s*", "", subject).strip()
    return s[:1].lower() + s[1:] if s else s


def build_message(part: Part, subject: str, orig_sha: str, index: int, total: int) -> str:
    ctype = commit_type(part.area, {c.status for c in part.changes}, subject)
    head = f"{ctype}({AREA_LABEL[part.area]}): {verb_for(part.changes)} {short_names(part.changes)}"
    context = clean_subject(subject)
    full = f"{head} — {context}" if context else head
    if len(full) > 110:
        full = full[:107].rstrip() + "..."
    files = "\n".join(f"- {c.status} {c.path}" for c in part.changes if not c.path.endswith(".meta"))
    body = f"Parte {index}/{total} de {orig_sha[:7]}: {subject}\n\nArchivos:\n{files}\n"
    return f"{full}\n\n{body}\nCo-Authored-By: Claude Opus 5.5 <noreply@anthropic.com>\n"


def split_parts(changes: list[Change]) -> list[Part]:
    parts: dict[tuple[str, str], Part] = {}
    for c in changes:
        owner = owner_of(c.path)
        area, key = group_of(c.path)
        parts.setdefault((owner, key), Part(owner, area, key)).changes.append(c)
    order = {a: i for i, a in enumerate(AREA_ORDER)}
    return sorted(parts.values(), key=lambda p: (order.get(p.area, 99), p.key, p.owner))


def parse_date(s: str) -> datetime:
    return datetime.fromisoformat(s.strip())


def fmt_date(d: datetime) -> str:
    return d.isoformat()


# ---------------------------------------------------------------------------------------------
# Programa
# ---------------------------------------------------------------------------------------------


def main() -> None:
    ap = argparse.ArgumentParser(description=__doc__, formatter_class=argparse.RawDescriptionHelpFormatter)
    ap.add_argument("--base", required=True, help="rama o commit base (p. ej. origin/main)")
    ap.add_argument("--source", required=True, help="rama fuente con todo el trabajo")
    ap.add_argument("--branch", action="append", required=True,
                    help="dueño=rama, p. ej. jose=jose/base-juego (una por dueño)")
    ap.add_argument("--dry-run", action="store_true", help="solo muestra el reparto")
    args = ap.parse_args()

    branches = dict(b.split("=", 1) for b in args.branch)
    for o in AUTHORS:
        if o not in branches:
            sys.exit(f"Falta --branch {o}=...")

    authors = dict(AUTHORS)
    authors["jose"] = (git("config", "user.name").strip(), git("config", "user.email").strip())

    base = git("rev-parse", args.base + "^{commit}").strip()
    source = git("rev-parse", args.source + "^{commit}").strip()
    merge_base = git("merge-base", base, source, check=False).strip()

    # Lista de (sha original, asunto, fecha, cambios)
    originals: list[tuple[str, str, datetime, list[Change]]] = []
    if merge_base:
        if merge_base != base:
            print(f"Aviso: la base {args.base} tiene commits que la fuente no tiene; se reparte base..fuente "
                  f"desde el ancestro común {merge_base[:7]}.")
        revs = git("rev-list", "--reverse", "--first-parent", f"{merge_base}..{source}").split()
        for sha in revs:
            info = git("show", "-s", "--format=%s%x00%aI", sha)
            subject, date = info.split("\0")
            parent = git("rev-parse", sha + "^1").strip()
            originals.append((sha, subject, parse_date(date), diff_tree(parent, sha)))
        start_tree_commit = merge_base
    else:
        print("La fuente no comparte historia con la base: se reparte el diff total por carpetas.")
        info = git("show", "-s", "--format=%s%x00%aI", source)
        subject, date = info.split("\0")
        originals.append((source, "integra el trabajo de " + args.source, parse_date(date), diff_tree(base, source)))
        start_tree_commit = base

    tips = {o: base for o in AUTHORS}  # cada rama sale de la base, para el PR
    last_date: dict[str, datetime | None] = {o: None for o in AUTHORS}
    counts = {o: 0 for o in AUTHORS}

    with tempfile.TemporaryDirectory() as tmp:
        index_env = {}
        for o in AUTHORS:
            idx = os.path.join(tmp, f"index_{o}")
            index_env[o] = {"GIT_INDEX_FILE": idx}
            git("read-tree", tips[o], env=index_env[o])

        for sha, subject, date, changes in originals:
            parts = split_parts(changes)
            for i, part in enumerate(parts):
                o = part.owner
                if args.dry_run:
                    print(f"[{sha[:7]}] {o:6} {part.area:12} {len(part.changes):3} archivos  {part.key}")
                    continue
                env = index_env[o]
                for c in part.changes:
                    if c.status == "D":
                        git("update-index", "--force-remove", "--", c.path, env=env)
                    else:
                        git("update-index", "--add", "--cacheinfo", f"{c.mode},{c.sha},{c.path}", env=env)
                tree = git("write-tree", env=env).strip()
                parent_tree = git("rev-parse", tips[o] + "^{tree}").strip()
                if tree == parent_tree:
                    continue  # nunca commits vacíos
                d = date + timedelta(minutes=i)
                if last_date[o] is not None and d <= last_date[o]:
                    d = last_date[o] + timedelta(seconds=30)
                last_date[o] = d
                name, email = authors[o]
                cenv = {"GIT_AUTHOR_NAME": name, "GIT_AUTHOR_EMAIL": email, "GIT_AUTHOR_DATE": fmt_date(d),
                        "GIT_COMMITTER_NAME": name, "GIT_COMMITTER_EMAIL": email, "GIT_COMMITTER_DATE": fmt_date(d)}
                msg = build_message(part, subject, sha, i + 1, len(parts))
                new = git("commit-tree", tree, "-p", tips[o], env=cenv, input_bytes=msg.encode("utf-8")).strip()
                tips[o] = new
                counts[o] += 1

    if args.dry_run:
        return

    for o, br in branches.items():
        git("update-ref", f"refs/heads/{br}", tips[o])
        print(f"{br}: {counts[o]} commits de {authors[o][0]} <{authors[o][1]}>")

    # Verificación: fusionar las 3 ramas debe dar el mismo árbol que la fuente.
    temp = "tmp/verificar-reparto"
    merged = tips["jose"]
    for o in ("majo", "daniel"):
        r = subprocess.run(["git", "merge-tree", "--write-tree", merged, tips[o]], capture_output=True)
        if r.returncode != 0:
            sys.exit(f"Conflicto al fusionar {branches[o]}:\n{r.stdout.decode()}")
        tree = r.stdout.decode().split()[0]
        merged = git("commit-tree", tree, "-p", merged, "-p", tips[o],
                     input_bytes=f"Verificación: fusiona {branches[o]}".encode()).strip()
    git("update-ref", f"refs/heads/{temp}", merged)
    diff = git("diff", "--stat", source, temp)
    git("update-ref", "-d", f"refs/heads/{temp}")
    if diff.strip():
        sys.exit("ERROR: la fusión de las ramas no es idéntica a la fuente:\n" + diff)
    print(f"OK: fusionar las {len(branches)} ramas da un árbol idéntico a {args.source} (git diff vacío).")


if __name__ == "__main__":
    main()
