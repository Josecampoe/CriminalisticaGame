# Proxy de IA — El Caso 404

Servidor mínimo (Node 18 o superior, sin dependencias) que conecta los diálogos del juego con la IA. Su razón de ser es que **la clave de API nunca va dentro del juego**, porque cualquier build se puede descompilar.

## Cómo usarlo

1. Define la clave como variable de entorno. Hazlo solo en tu máquina o en el servidor, nunca en archivos del repositorio.
   - PowerShell: `$env:ANTHROPIC_API_KEY = "tu-clave"`
2. Arranca el proxy: `node Server/proxy.mjs`. Escucha en `http://localhost:8787/dialogue`.
3. En Unity, abre `Assets/_Project/Data/AiSettings` y **desmarca *Use Offline***. La URL por defecto ya apunta a `http://localhost:8787/dialogue`.

**Sin clave**, el proxy arranca en **modo de prueba**: responde con un texto fijo y no llama a la IA. Sirve para comprobar la conexión desde el juego.

## Variables de entorno

| Variable | Qué hace |
|---|---|
| `ANTHROPIC_API_KEY` | Clave de API. Si falta, arranca en modo de prueba. |
| `ANTHROPIC_MODEL` | Modelo (por defecto `claude-haiku-5-5`, rápido y económico para diálogos). |
| `PORT` | Puerto (por defecto `8787`). |
| `CASO404_MOCK=1` | Fuerza el modo de prueba aunque haya clave. |

## Qué hace

1. Recibe del juego el perfil del personaje: personalidad, hechos que conoce, mentiras permitidas y ejemplos de cómo habla. También recibe la escena, las evidencias analizadas y las últimas líneas de la conversación.
2. Pide a la Messages API una respuesta en **JSON con esquema fijo** (structured outputs): `reply`, `emotion`, `usedFactIds`, `usedLieIds` y `revealedSecretIds`.
3. Si la IA falla, se niega o la respuesta llega cortada, devuelve un error. El juego entonces usa la frase de respaldo del guion.

El juego también **valida** cada respuesta (`AiReplyValidator`): los IDs tienen que existir y los secretos tienen que estar desbloqueados. La IA solo pone la voz; la verdad del caso y los finales siguen saliendo de los datos.
