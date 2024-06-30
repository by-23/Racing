using System;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.Serialization;

namespace Ilumisoft.SkillDrive
{
    public class Vehicle : NetworkBehaviour
    {
        private bool isLocalPlayer;

        [SerializeField] internal Camera playerCam;

        [SerializeField] private TriggerCallBack triggerCallback;

        [SerializeField] VehicleStats stats = new VehicleStats();

        [SerializeField] VehiclePhysics physics = new VehiclePhysics();

        [SerializeField] VehicleGroundDetection groundDetection = new VehicleGroundDetection();

        private Joystick joystick;

        public VehicleStats FinalStats => stats;
        public Rigidbody Rigidbody { get; private set; }
        public bool IsGrounded => groundDetection.IsGrounded;
        public bool CanMove { get; set; } = true;
        public float ForwardSpeed => Vector3.Dot(Rigidbody.velocity, transform.forward);

        public float NormalizedForwardSpeed
        {
            get => (Mathf.Abs(ForwardSpeed) > 0.1f) ? ForwardSpeed / FinalStats.MaxSpeed : 0.0f;
        }

        protected virtual void Awake()
        {
            Rigidbody = GetComponent<Rigidbody>();
            joystick = Joystick.Instance;
            AccelerationButtons.Instance.vehicle = this;
            groundDetection.Initialize(this);
            triggerCallback.OnTriggerEntered += OnTriggerEntered;
        }

        public void SetLocalPlayer()
        {
            isLocalPlayer = true;
            playerCam.gameObject.SetActive(true);
        }

        protected virtual void FixedUpdate()
        {
            if (isLocalPlayer)
            {
                PerformGroundCheck();

                ApplyGravity();

                ApplyLateralFriction();

                ApplySteering();

                // ApplyAcceleration();
            }
        }


        private void OnTriggerEntered(Collider other)
        {
            if (other.TryGetComponent(out Projectile projectile))
                projectile.SetTarget(this);
        }


        protected virtual void PerformGroundCheck()
        {
            groundDetection.CheckGround();
        }


        protected virtual void ApplyGravity()
        {
            float factor = groundDetection.IsGrounded ? physics.Gravity : physics.FallGravity;

            Rigidbody.AddForce(-factor * Vector3.up, ForceMode.Acceleration);
        }

        protected virtual void ApplyLateralFriction()
        {
            if (IsGrounded)
            {
                // Calculate how much the vehicle is moving left or right
                float lateralSpeed = Vector3.Dot(Rigidbody.velocity, transform.right);

                //Calculate the desired amount of friction to apply to the side of the vehicle.
                Vector3 lateralFriction = -transform.right * ((lateralSpeed / Time.fixedDeltaTime) * FinalStats.Grip);

                Rigidbody.AddForce(lateralFriction, ForceMode.Acceleration);
            }
        }

        protected virtual void ApplySteering()
        {
            if (IsGrounded && CanMove)
            {
                float steeringPower;
#if UNITY_STANDALONE || UNITY_WEBGL
        // Используем стандартное управление для ПК
        steeringPower = UnityEngine.Input.GetAxis("Horizontal") * FinalStats.SteeringPower;
#elif UNITY_ANDROID || UNITY_IOS
                // Используем джойстик для мобильных устройств
                steeringPower = joystick.Horizontal * FinalStats.SteeringPower;
#endif

                float speedFactor = ForwardSpeed * 0.075f;
                steeringPower = Mathf.Clamp(steeringPower * speedFactor, -FinalStats.SteeringPower,
                    FinalStats.SteeringPower);
                float rotationTorque = steeringPower - Rigidbody.angularVelocity.y;
                Rigidbody.AddRelativeTorque(0f, rotationTorque, 0f, ForceMode.VelocityChange);
            }
        }

        public virtual void ApplyAcceleration(float accelerationInput)
        {
            if (IsGrounded && CanMove)
            {
                float forceMagnitude = 0f; // Инициализируем переменную для хранения величины силы

#if UNITY_STANDALONE || UNITY_WEBGL
        // Используем стандартное управление для ПК
        forceMagnitude = FinalStats.Acceleration * UnityEngine.Input.GetAxis("Vertical");
#elif UNITY_ANDROID || UNITY_IOS
                // Используем джойстик для мобильных устройств
                forceMagnitude = accelerationInput * FinalStats.Acceleration;
#endif

                // Применяем силу для ускорения
                Rigidbody.AddForce(transform.forward * forceMagnitude, ForceMode.Acceleration);

                // Опционально: ограничение максимальной скорости и плавное торможение
                var currentSpeed = Rigidbody.velocity.magnitude;
                var maxSpeed = FinalStats.MaxSpeed;
                if (currentSpeed > maxSpeed)
                {
                    // Применяем обратную силу для уменьшения скорости до максимально допустимой
                    var excessSpeed = currentSpeed - maxSpeed;
                    Rigidbody.AddForce(-transform.forward * (forceMagnitude * (excessSpeed / maxSpeed)),
                        ForceMode.Acceleration);
                }
            }
        }

        public void Reset()
        {
            // Automatically add and setup a rigidbody if none exists
            if (GetComponent<Rigidbody>() == null)
            {
                var rb = gameObject.AddComponent<Rigidbody>();

                rb.interpolation = RigidbodyInterpolation.None;
                rb.useGravity = false;
                rb.collisionDetectionMode = CollisionDetectionMode.ContinuousDynamic;
                rb.mass = 2500;
                rb.angularDrag = 3;
                rb.drag = 0.05f;
            }
        }

        private void OnDestroy()
        {
            triggerCallback.OnTriggerEntered += OnTriggerEntered;
        }

        private void OnDrawGizmosSelected()
        {
            groundDetection.OnDrawGizmosSelected(this);
        }
    }
}