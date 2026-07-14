using UnityEngine;

namespace SpeedRush
{
    public class MobileRacingInput : MonoBehaviour
    {
        [Header("SimpleInput Axis Names")]
        public string steerLeftAxis = "SteerLeft";
        public string steerRightAxis = "SteerRight";
        public string gasAxis = "Vertical";

        [Header("Steering")]
        public float steerSensitivity = 1f;
        public float steerFilterSpeed = 10f;

        [Header("Brake/Reverse")]
        public float brakeReverseThreshold = 2f;
        public float reversePower = 0.9f;

        public float SteerInput { get; private set; }
        public float MoveInput { get; private set; }
        public bool IsBraking { get; private set; }

        private Rigidbody carRb;

        void Awake()
        {
            carRb = GetComponent<Rigidbody>();
            if (carRb == null)
                carRb = GetComponentInParent<Rigidbody>();
        }

        void Update()
        {
            float left = SimpleInput.GetAxis(steerLeftAxis);
            float right = SimpleInput.GetAxis(steerRightAxis);
            float rawTarget = Mathf.Clamp(right - left, -1f, 1f);

            if (Mathf.Abs(rawTarget) > 0.01f)
                SteerInput = rawTarget * steerSensitivity;
            else
                SteerInput = Mathf.MoveTowards(SteerInput, 0f, steerFilterSpeed * Time.deltaTime);

            float rawVertical = SimpleInput.GetAxis(gasAxis);
            ProcessGasBrake(rawVertical);
        }

        void ProcessGasBrake(float rawVertical)
        {
            if (rawVertical > 0.1f)
            {
                MoveInput = rawVertical;
                IsBraking = false;
            }
            else if (rawVertical < -0.1f)
            {
                float speed = Vector3.Dot(carRb.linearVelocity, transform.forward);
                if (speed > brakeReverseThreshold)
                {
                    MoveInput = 0f;
                    IsBraking = true;
                }
                else
                {
                    MoveInput = -reversePower;
                    IsBraking = false;
                }
            }
            else
            {
                MoveInput = 0f;
                IsBraking = false;
            }
        }
    }
}
