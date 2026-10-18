using Caso404.Core;
using Caso404.Data;
using Caso404.Interaction;
using Caso404.Player;
using UnityEngine;

namespace Caso404.Dialogue
{
    /// <summary>Personaje 3D con el que se puede hablar. Gira hacia el jugador mientras conversan.</summary>
    public class CharacterNpc : MonoBehaviour, IInteractable
    {
        [SerializeField] private CharacterProfile _profile;
        [SerializeField] private Transform _head;
        [SerializeField] private float _turnSpeed = 4f;

        Transform _lookTarget;
        bool _talkedOnce;

        public CharacterProfile Profile => _profile;
        public Transform Head => _head != null ? _head : transform;

        public string Prompt => $"Hablar con {_profile.DisplayName}";
        public bool CanInteract => _profile != null;
        public bool IsNew => !_talkedOnce;

        void Awake()
        {
            if (_profile == null) GameLog.Error($"{name}: asigna un CharacterProfile.", this);
        }

        public void Interact(PlayerContext ctx)
        {
            _talkedOnce = true;
            _lookTarget = ctx.Camera != null ? ctx.Camera.transform : ctx.transform;
            ctx.RequestTalk(_profile, Head);
        }

        /// <summary>Deja de mirar al jugador (al cerrar el diálogo).</summary>
        public void StopLooking() => _lookTarget = null;

        void Update()
        {
            if (_lookTarget == null) return;
            var dir = _lookTarget.position - transform.position;
            dir.y = 0f;
            if (dir.sqrMagnitude < 0.01f) return;
            transform.rotation = Quaternion.Slerp(transform.rotation, Quaternion.LookRotation(dir), Time.deltaTime * _turnSpeed);
        }
    }
}
