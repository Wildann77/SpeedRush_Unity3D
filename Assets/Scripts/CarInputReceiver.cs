using UnityEngine;
using PolyStang;

namespace SpeedRush
{
    public class CarInputReceiver : MonoBehaviour
    {
        public MobileRacingInput mobileInput;
        public PolyStang.CarController carController;

        void Awake()
        {
            if (mobileInput == null)
                mobileInput = GetComponent<MobileRacingInput>();
            if (carController == null)
                carController = GetComponent<PolyStang.CarController>();
        }

        void FixedUpdate()
        {
            if (mobileInput == null || carController == null) return;

            carController.SteerInput(mobileInput.SteerInput);
            carController.MoveInput(mobileInput.MoveInput);
            carController.BrakeInput(mobileInput.IsBraking ? 1f : 0f);
        }
    }
}
