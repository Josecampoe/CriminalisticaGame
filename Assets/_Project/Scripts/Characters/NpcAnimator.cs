using Caso404.Dialogue;
using UnityEngine;

namespace Caso404.Characters
{
    /// <summary>
    /// Anima el modelo humanoide de un personaje: reposo (de pie o sentado), animación de hablar mientras conversa
    /// y la cabeza que mira al jugador (IK de mirada). Va en el mismo objeto que el Animator.
    /// </summary>
    [RequireComponent(typeof(Animator))]
    public class NpcAnimator : MonoBehaviour
    {
        static readonly int TalkingId = Animator.StringToHash("Talking");
        static readonly int SeatedId = Animator.StringToHash("Seated");
        static readonly int CycleOffsetId = Animator.StringToHash("CycleOffset");

        [SerializeField] private CharacterNpc _npc;
        [SerializeField] private bool _seated;
        [SerializeField, Range(0f, 1f)] private float _maxLookWeight = 0.85f;
        [SerializeField] private float _lookBlendSpeed = 3f;

        Animator _animator;
        bool _talking;
        float _lookWeight;
        Vector3 _lookPoint;

        /// <summary>True si el personaje está sentado (lo fija el generador de escenas).</summary>
        public bool Seated => _seated;

        void Awake()
        {
            _animator = GetComponent<Animator>();
        }

        void Start()
        {
            _animator.SetBool(SeatedId, _seated);
            // Cada personaje respira a su ritmo: desfasa el ciclo de reposo para que no se muevan todos a la vez.
            _animator.SetFloat(CycleOffsetId, Mathf.Repeat(transform.position.x * 0.37f + transform.position.z * 0.61f, 1f));
        }

        void Update()
        {
            bool talking = _npc != null && _npc.IsTalking;
            if (talking != _talking)
            {
                _talking = talking;
                _animator.SetBool(TalkingId, talking);
            }
            var target = _npc != null ? _npc.LookTarget : null;
            if (target != null) _lookPoint = target.position;
            _lookWeight = Mathf.MoveTowards(_lookWeight, target != null ? _maxLookWeight : 0f, Time.deltaTime * _lookBlendSpeed);
        }

        void OnAnimatorIK(int layerIndex)
        {
            if (_lookWeight <= 0.001f) { _animator.SetLookAtWeight(0f); return; }
            _animator.SetLookAtWeight(_lookWeight, 0.15f, 0.9f, 0f, 0.6f);
            _animator.SetLookAtPosition(_lookPoint);
        }
    }
}
