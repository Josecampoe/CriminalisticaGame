using UnityEngine;

namespace Caso404.Data
{
    /// <summary>Flag de historia (ej. flag_c1_carlos_detenido). Un asset por flag evita errores de tipeo.</summary>
    [CreateAssetMenu(menuName = "Caso404/Flag", fileName = "flag_")]
    public class FlagData : ScriptableObject
    {
        [SerializeField] private string _id;
        [SerializeField, TextArea] private string _description;
        [SerializeField] private bool _persistsToNextCase;

        public string Id { get => _id; internal set => _id = value; }
        public string Description { get => _description; internal set => _description = value; }
        /// <summary>Si pasa del caso 1 al caso 2 (GUION §2).</summary>
        public bool PersistsToNextCase { get => _persistsToNextCase; internal set => _persistsToNextCase = value; }
    }
}
