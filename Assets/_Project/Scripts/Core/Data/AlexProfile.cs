using UnityEngine;

namespace Caso404.Data
{
    /// <summary>
    /// Perfil de ALEX. Además de lo de un personaje, tiene reglas de confianza (alexTrust) y
    /// sus respuestas al ser aceptado o cuestionado (GUION §1 y §3.5).
    /// </summary>
    [CreateAssetMenu(menuName = "Caso404/ALEX", fileName = "chr_alex")]
    public class AlexProfile : CharacterProfile
    {
        [SerializeField] private int _startTrust = 60;
        [SerializeField] private int _highTrustThreshold = 60;
        [SerializeField] private int _lowTrustThreshold = 30;
        [SerializeField] private int _acceptTrustDelta = 5;
        [SerializeField] private int _questionSuccessTrustDelta = -10;
        [SerializeField, TextArea] private string _questionedSuccessHigh;
        [SerializeField, TextArea] private string _questionedSuccessLow;
        [SerializeField, TextArea] private string _questionedFail;
        [SerializeField, TextArea] private string _acceptedLine;

        public int StartTrust { get => _startTrust; internal set => _startTrust = value; }
        /// <summary>Con confianza ≥ este valor ALEX es cálido.</summary>
        public int HighTrustThreshold { get => _highTrustThreshold; internal set => _highTrustThreshold = value; }
        /// <summary>Por debajo de este valor ALEX es frío y su panel hace glitch.</summary>
        public int LowTrustThreshold { get => _lowTrustThreshold; internal set => _lowTrustThreshold = value; }
        public int AcceptTrustDelta { get => _acceptTrustDelta; internal set => _acceptTrustDelta = value; }
        public int QuestionSuccessTrustDelta { get => _questionSuccessTrustDelta; internal set => _questionSuccessTrustDelta = value; }
        public string QuestionedSuccessHigh { get => _questionedSuccessHigh; internal set => _questionedSuccessHigh = value; }
        public string QuestionedSuccessLow { get => _questionedSuccessLow; internal set => _questionedSuccessLow = value; }
        public string QuestionedFail { get => _questionedFail; internal set => _questionedFail = value; }
        public string AcceptedLine { get => _acceptedLine; internal set => _acceptedLine = value; }
    }
}
