using UnityEngine;
using System;
using System.Collections.Generic;
using TMPro;
using Cinemachine;

namespace PolyStang
{
    public class CarController : MonoBehaviour
    {
        public enum ControlMode // this car controller works for both pc and touch devices. You can switch the control mode from the inspector.
        {
            Keyboard,
            Buttons
        };

        public enum Axel // used to identify front and rear wheels.
        {
            Front,
            Rear
        }

        [Serializable]
        public struct Wheel // wheel bits: all fields must be filled to make the wheel work properly.
        {
            public GameObject wheelModel;
            public WheelCollider wheelCollider;
            public GameObject wheelEffectObj;
            public ParticleSystem smokeParticle;
            public Axel axel;
            public GameObject skidSound;
            public int index;
        }

        public ControlMode control;

        [Header("Inputs")]
        public KeyCode brakeKey = KeyCode.LeftShift;

        [Header("Accelerations and deaccelerations")]
        public float maxAcceleration = 30.0f;
        public float brakeAcceleration = 50.0f;
        public float noInputDeacceleration = 10.0f;

        [Header("Steering")]
        public float turnSensitivity = 0.8f;
        public float maxSteerAngle = 20.0f; // Dikurangi agar tidak terlalu tajam beloknya yang bikin drift
        public float steerFilterSpeed = 2.0f; // Kecepatan respon setir diperhalus agar tidak snap seketika

        [Header("Speed UI")]
        public TMP_Text speedText;
        public float UISpeedMultiplier = 4;

        [Header("Speed limit")]
        public float frontMaxSpeed = 200;
        public float rearMaxSpeed = 50;
        public float empiricalCoefficient = 0.41f;
        public enum TypeOfSpeedLimit
        {
            noSpeedLimit,
            simple,
            squareRoot
        };
        public TypeOfSpeedLimit typeOfSpeedLimit = TypeOfSpeedLimit.squareRoot;
        private float frontSpeedReducer = 1;
        private float rearSpeedReducer = 1;

        [Header("Skid")]
        public float brakeDriftingSkidLimit = 10f;
        public float lateralFrontDriftingSkidLimit = 0.6f;
        public float lateralRearDriftingSkidLimit = 0.3f;

        [Header("Stability")]
        [Range(0, 1)] public float steerSpeedReduction = 0.25f; // Mengurangi radius putar secara signifikan di kecepatan tinggi
        public float antiRollForce = 6000f; // Menjaga mobil tetap stabil saat berbelok tajam
        public float downforce = 200f; // Tekanan ke bawah ditingkatkan agar ban lebih menggigit aspal
        public float sidewaysStiffness = 3.5f; // Ban mencengkeram aspal jauh lebih erat untuk menghindari slip instan
        public Vector3 _centerOfMass;

        public List<Wheel> wheels;

        float moveInput;
        float steerInput;
        private float keyboardSteerTarget; // Digunakan untuk smoothing input keyboard

        private Rigidbody carRb;

        private CarLights carLights;
        private CarSounds carSounds;

        void Start() // called the first frame, when the game starts.
        {
            carRb = GetComponent<Rigidbody>();
            carRb.centerOfMass = _centerOfMass;

            carLights = GetComponent<CarLights>();
            carSounds = GetComponent<CarSounds>();

            SetupCamera();
        }

        void SetupCamera()
        {
            // Pastikan Main Camera punya CinemachineBrain
            Camera mainCam = Camera.main;
            if (mainCam == null) mainCam = GameObject.FindAnyObjectByType<Camera>();
            if (mainCam == null) return;

            mainCam.tag = "MainCamera";
            if (mainCam.GetComponent<CinemachineBrain>() == null)
            {
                mainCam.gameObject.AddComponent<CinemachineBrain>();
            }

            // Buat Virtual Camera secara otomatis jika belum ada yang menempel
            CinemachineVirtualCamera vcam = GameObject.FindAnyObjectByType<CinemachineVirtualCamera>();
            if (vcam == null)
            {
                GameObject vcamObj = new GameObject("Auto Follow Camera", typeof(CinemachineVirtualCamera));
                vcam = vcamObj.GetComponent<CinemachineVirtualCamera>();
            }

            vcam.Follow = transform;
            vcam.LookAt = transform;
            vcam.m_Priority = 1000;

            var transposer = vcam.GetCinemachineComponent<CinemachineTransposer>();
            if (transposer == null) transposer = vcam.AddCinemachineComponent<CinemachineTransposer>();
            transposer.m_BindingMode = CinemachineTransposer.BindingMode.LockToTargetWithWorldUp;
            transposer.m_FollowOffset = new Vector3(0, 3.5f, -10f);

            var composer = vcam.GetCinemachineComponent<CinemachineComposer>();
            if (composer == null) composer = vcam.AddCinemachineComponent<CinemachineComposer>();
            composer.m_TrackedObjectOffset = new Vector3(0, 1.5f, 0);
            
            Debug.Log("Kamera otomatis disetup untuk mengikuti " + name);
        }

        void Update() // called every frame.
        {
            GetInputs();
            AnimateWheels();
            WheelEffectsCheck();
            CarLightsControl();
        }

        void FixedUpdate()
        {
            Move();
            Steer();
            BrakeAndDeacceleration();
            ApplyStability();
        }

        void LateUpdate() // called after the "Update()" function.
        {
            UpdateSpeedUI();
        }

        public void MoveInput(float input) // used for touch controls.
        {
            moveInput = input;
        }

        public void SteerInput(float input) // used for touch controls.
        {
            steerInput = input;
        }

        void GetInputs() // inputs.
        {
            if (control == ControlMode.Keyboard)
            {
                moveInput = Input.GetAxis("Vertical");
                // Menggunakan GetAxis yang memiliki filtering bawaan Unity untuk belok yang lebih halus
                keyboardSteerTarget = Input.GetAxis("Horizontal");
                steerInput = Mathf.MoveTowards(steerInput, keyboardSteerTarget, steerFilterSpeed * Time.deltaTime);
            }
        }

        void Move() // main vertical acceleration.
        {
            foreach (var wheel in wheels)
            {
                // rotational speed is proportional to radius * frequency: the empirical coefficient is around 0.41
                float currentWheelSpeed = empiricalCoefficient * wheel.wheelCollider.radius * wheel.wheelCollider.rpm;

                if (moveInput > 0 || currentWheelSpeed > 0) // when moving forwards
                { 
                    if(currentWheelSpeed > frontMaxSpeed) // important check: it prevents the car from accelerating indefinetly
                    {
                        currentWheelSpeed = frontMaxSpeed;
                    }
                    
                    // cases: different speed reducing technics
                    if (typeOfSpeedLimit == TypeOfSpeedLimit.noSpeedLimit)
                    {
                        frontSpeedReducer = 1;
                    }
                    else if (typeOfSpeedLimit == TypeOfSpeedLimit.simple)
                    {
                        frontSpeedReducer = (frontMaxSpeed - currentWheelSpeed ) / frontMaxSpeed;
                    }
                    else if (typeOfSpeedLimit == TypeOfSpeedLimit.squareRoot)
                    {
                        frontSpeedReducer = Mathf.Sqrt(Mathf.Abs((frontMaxSpeed - currentWheelSpeed) / frontMaxSpeed));
                    }

                    // applying reduction
                    wheel.wheelCollider.motorTorque = moveInput * 600 * maxAcceleration * frontSpeedReducer;
                }
                else if (moveInput < 0 || currentWheelSpeed < 0) // when moving backwards
                {
                    if (currentWheelSpeed < - rearMaxSpeed) // important check: it prevents the car from accelerating indefinetly
                    {
                        currentWheelSpeed = - rearMaxSpeed;
                    }

                    // cases: different speed reducing technics
                    if (typeOfSpeedLimit == TypeOfSpeedLimit.noSpeedLimit)
                    {
                        rearSpeedReducer = 1;
                    }
                    else if (typeOfSpeedLimit == TypeOfSpeedLimit.simple)
                    {
                        rearSpeedReducer = (rearMaxSpeed + currentWheelSpeed) / rearMaxSpeed;
                    }
                    else if (typeOfSpeedLimit == TypeOfSpeedLimit.squareRoot)
                    {
                        rearSpeedReducer = Mathf.Sqrt(Mathf.Abs((rearMaxSpeed + currentWheelSpeed) / rearMaxSpeed));
                    }

                    // applying reduction
                    wheel.wheelCollider.motorTorque = moveInput * 600 * maxAcceleration * rearSpeedReducer;
                }
            }
        }

        void Steer() // to rotate the front wheels, when steering.
        {
            // Speed-sensitive steering: makin cepat mobil, makin kecil sudut beloknya secara non-linear agar tidak drift berlebihan
            float currentSpeed = carRb.linearVelocity.magnitude * 3.6f;
            float speedRatio = Mathf.Clamp01(currentSpeed / frontMaxSpeed);
            // Menggunakan fungsi pangkat 0.5 (akar kuadrat) agar pengurangan sudut belok terjadi secara agresif di kecepatan menengah
            float speedFactor = Mathf.Pow(speedRatio, 0.5f);
            float currentMaxSteerAngle = Mathf.Lerp(maxSteerAngle, maxSteerAngle * steerSpeedReduction, speedFactor);

            foreach (var wheel in wheels)
            {
                if (wheel.axel == Axel.Front)
                {
                    var _steerAngle = steerInput * turnSensitivity * currentMaxSteerAngle;
                    // Lerp dikurangi dari 0.6 ke 0.1 agar setir tidak "menghentak" (snap) yang bikin drift
                    wheel.wheelCollider.steerAngle = Mathf.Lerp(wheel.wheelCollider.steerAngle, _steerAngle, 0.1f);
                }

                // Tingkatkan cengkeraman ban secara agresif
                WheelFrictionCurve sf = wheel.wheelCollider.sidewaysFriction;
                sf.stiffness = sidewaysStiffness;
                wheel.wheelCollider.sidewaysFriction = sf;

                WheelFrictionCurve ff = wheel.wheelCollider.forwardFriction;
                ff.stiffness = sidewaysStiffness * 0.8f; // Juga tingkatkan forward friction agar tidak spin di tempat
                wheel.wheelCollider.forwardFriction = ff;
            }
        }

        void ApplyStability()
        {
            // Apply Downforce
            carRb.AddForce(-transform.up * downforce * carRb.linearVelocity.magnitude);

            // Anti-Roll Bar Logic
            // Kita hitung gaya untuk menyeimbangkan mobil kiri-kanan
            if (wheels.Count >= 4)
            {
                ApplyAntiRoll(wheels[0].wheelCollider, wheels[1].wheelCollider); // Front
                ApplyAntiRoll(wheels[2].wheelCollider, wheels[3].wheelCollider); // Rear
            }
        }

        void ApplyAntiRoll(WheelCollider leftWheel, WheelCollider rightWheel)
        {
            WheelHit hit;
            float travelL = 1.0f;
            float travelR = 1.0f;

            bool groundedL = leftWheel.GetGroundHit(out hit);
            if (groundedL) travelL = (-leftWheel.transform.InverseTransformPoint(hit.point).y - leftWheel.radius) / leftWheel.suspensionDistance;

            bool groundedR = rightWheel.GetGroundHit(out hit);
            if (groundedR) travelR = (-rightWheel.transform.InverseTransformPoint(hit.point).y - rightWheel.radius) / rightWheel.suspensionDistance;

            float antiRollForceAmount = (travelL - travelR) * antiRollForce;

            if (groundedL) carRb.AddForceAtPosition(leftWheel.transform.up * -antiRollForceAmount, leftWheel.transform.position);
            if (groundedR) carRb.AddForceAtPosition(rightWheel.transform.up * antiRollForceAmount, rightWheel.transform.position);
        }

        void BrakeAndDeacceleration()
        {
            if (Input.GetKey(brakeKey)) // when pressing space, the brake is used.
            {
                foreach (var wheel in wheels)
                {
                    wheel.wheelCollider.brakeTorque = 300 * brakeAcceleration;
                }

            }
            else if (moveInput == 0) // with no vertical input, a slight deacceleration is used to slightly slow down the speed of the car.
            {
                foreach (var wheel in wheels)
                {
                    wheel.wheelCollider.brakeTorque = 300 * noInputDeacceleration;
                }
            }
            else // with vertical input, no brake or deacceleration is applied.
            {
                foreach (var wheel in wheels)
                {
                    wheel.wheelCollider.brakeTorque = 0;
                }
            }
        }

        void AnimateWheels() // to animate wheels accordingly to the car speed.
        {
            foreach (var wheel in wheels)
            {
                Quaternion rot;
                Vector3 pos;
                wheel.wheelCollider.GetWorldPose(out pos, out rot);
                wheel.wheelModel.transform.position = pos;
                wheel.wheelModel.transform.rotation = rot;
            }
        }

        void WheelEffectsCheck() // checking for every wheel if it's slipping: if yes, the "EffectCreate()" function is called.
        {
            foreach (var wheel in wheels)
            {
                // slipping ---> skid
                WheelHit GroundHit; // variable to store hit data
                wheel.wheelCollider.GetGroundHit(out GroundHit); // store hit data into GroundHit
                float lateralDrift = Mathf.Abs(GroundHit.sidewaysSlip);

                if (Input.GetKey(brakeKey) && wheel.axel == Axel.Rear && wheel.wheelCollider.isGrounded == true && carRb.linearVelocity.magnitude >= brakeDriftingSkidLimit)
                {
                    EffectCreate(wheel);
                }
                else if (wheel.wheelCollider.isGrounded == true && wheel.axel == Axel.Front && (lateralDrift > lateralFrontDriftingSkidLimit)) // drifting: front wheels
                {
                    EffectCreate(wheel);
                }
                else if (wheel.wheelCollider.isGrounded == true && wheel.axel == Axel.Rear && (lateralDrift > lateralRearDriftingSkidLimit)) // drifting: rear wheels
                {
                    EffectCreate(wheel);
                }
                else
                {
                    wheel.wheelEffectObj.GetComponentInChildren<TrailRenderer>().emitting = false;
                    carSounds.StopSkidSound(wheel.skidSound, wheel.index); // actually decreasing the volume of the skid to 0: see the "CarSound" script.
                }
            }
        }

        private void EffectCreate(Wheel wheel) // actually creating the effects: 1) trail renderer for the skid, 2) smoke particles, 3) skid sound.
        {
            wheel.wheelEffectObj.GetComponentInChildren<TrailRenderer>().emitting = true;
            wheel.smokeParticle.Emit(1);
            carSounds.PlaySkidSound(wheel.skidSound); // actually setting the volume of the skid to 1
        }

        void CarLightsControl() // controlling lights, through the specific script "CarSounds".
        {
            if (Input.GetKey(brakeKey)) // the red lights are activated when the brake is pressed
            {
                carLights.RearRedLightsOn();
            }
            else
            {
                carLights.RearRedLightsOff();
            }

            if (moveInput < 0f) // the rear white lights are activated when the player is pressing "S" or down arrow.
            {
                carLights.RearWhiteLightsOn();
            }
            else
            {
                carLights.RearWhiteLightsOff();
            }
        }

        void UpdateSpeedUI() // UI: speed update.
        {
            if (speedText == null) return;
            if (carRb == null) carRb = GetComponent<Rigidbody>();
            if (carRb == null) return;

            int roundedSpeed = (int)Mathf.Round(carRb.linearVelocity.magnitude * UISpeedMultiplier);
            speedText.text = roundedSpeed.ToString();
        }

        // --- SPEED BOOST (NITRO) FEATURE ---
        private float originalMaxAcceleration;
        private float originalFrontMaxSpeed;
        private bool isBoosting = false;
        private Coroutine boostCoroutine;

        public void ApplyBoost(float accelerationMultiplier, float speedMultiplier, float duration)
        {
            if (isBoosting)
            {
                StopCoroutine(boostCoroutine);
                // Kembalikan ke nilai awal sebelum me-restart boost agar multiplier tidak menumpuk
                maxAcceleration = originalMaxAcceleration;
                frontMaxSpeed = originalFrontMaxSpeed;
            }
            else
            {
                originalMaxAcceleration = maxAcceleration;
                originalFrontMaxSpeed = frontMaxSpeed;
                isBoosting = true;
            }
            boostCoroutine = StartCoroutine(BoostRoutine(accelerationMultiplier, speedMultiplier, duration));
        }

        private System.Collections.IEnumerator BoostRoutine(float accelerationMultiplier, float speedMultiplier, float duration)
        {
            maxAcceleration = originalMaxAcceleration * accelerationMultiplier;
            frontMaxSpeed = originalFrontMaxSpeed * speedMultiplier;
            
            // Berikan dorongan fisik instan (push) ke depan
            if (carRb != null)
            {
                carRb.AddForce(transform.forward * 12f, ForceMode.VelocityChange);
            }

            yield return new WaitForSeconds(duration);

            maxAcceleration = originalMaxAcceleration;
            frontMaxSpeed = originalFrontMaxSpeed;
            isBoosting = false;
        }
    }
}