using System;
using Unity.Netcode;
using UnityEngine;
using Photon.Pun;
using UnityEngine.Serialization;
using Random = UnityEngine.Random;

namespace Ilumisoft.SkillDrive
{
    public class Vehicle : NetworkBehaviour
    {
        [SerializeField] internal Camera playerCam;
        [SerializeField] private Attack attack;
        [SerializeField] private TriggerCallBack triggerCallback;
        [SerializeField] VehicleStats stats = new VehicleStats();
        [SerializeField] VehiclePhysics physics = new VehiclePhysics();
        [SerializeField] VehicleGroundDetection groundDetection = new VehicleGroundDetection();
        [SerializeField] private Transform FLwheel;
        [SerializeField] private Transform FRwheel;
        [SerializeField] private Transform BLwheel;
        [SerializeField] private Transform BRwheel;
        [SerializeField] private Transform FLwheelPivot;
        [SerializeField] private Transform FRwheelPivot;
        [SerializeField] private Transform BLwheelPivot;
        [SerializeField] private Transform BRwheelPivot;
        [SerializeField] private float wheelsRotationSpeed = 100f;
        [SerializeField] private float turnPercentage = 1f;
        [SerializeField] private float turnSpeed = 20f;


        private bool isLocalPlayer;
        private float currentTurnAngle = 0f;
        public HealthController healthController;
        public VehicleStats FinalStats => stats;
        public Rigidbody Rigidbody { get; private set; }
        public bool IsGrounded => groundDetection.IsGrounded;
        public bool CanMove = true;
        private FloatingJoystick joystick;
        public float ForwardSpeed => Vector3.Dot(Rigidbody.velocity, transform.forward);

        public float NormalizedForwardSpeed


        {
            get => (Mathf.Abs(ForwardSpeed) > 0.1f) ? ForwardSpeed / FinalStats.MaxSpeed : 0.0f;
        }

        protected virtual void Awake()
        {
            Rigidbody = GetComponent<Rigidbody>();
            groundDetection.Initialize(this);
            triggerCallback.OnTriggerEntered += OnTriggerEntered;
            healthController = GetComponent<HealthController>();
            healthController.OnDeath += OnDeath;
            joystick = FindObjectOfType<FloatingJoystick>();
        }

        protected virtual void FixedUpdate()
        {
            if (isLocalPlayer)
            {
                if (UnityEngine.Input.GetKeyDown(KeyCode.R))
                {
                    ResetCarPosition();
                }

                PerformGroundCheck();

                ApplyGravity();

                ApplyLateralFriction();

                ApplySteering();
            }
        }

        private void Update()
        {
            if (isLocalPlayer)
            {
#if UNITY_STANDALONE || UNITY_WEBGL
                float turnInput = UnityEngine.Input.GetAxisRaw("Horizontal");
#elif UNITY_ANDROID || UNITY_IOS
                float turnInput = joystick.Horizontal;
#endif
                TurnWheels(turnInput * 30f);
                RotateWheels();
            }
        }


        private void OnTriggerEntered(Collider other)
        {
            if (other.TryGetComponent(out Projectile projectile) && projectile.Owner != this)
                projectile.SetTarget(this);
        }

        private void OnDeath()
        {
            CanMove = false;
        }

        private void RotateWheels()
        {
            float rotationSpeed = ForwardSpeed * wheelsRotationSpeed * Time.deltaTime;
            FLwheel.Rotate(Vector3.right, rotationSpeed);
            FRwheel.Rotate(Vector3.right, rotationSpeed);
            BLwheel.Rotate(Vector3.right, rotationSpeed);
            BRwheel.Rotate(Vector3.right, rotationSpeed);
        }

        public void ApplyBraking(float brakePower)
        {
            Vector3 brakeForce = -Rigidbody.velocity.normalized * brakePower;
            Rigidbody.AddForce(brakeForce, ForceMode.Acceleration);
        }

        public void TurnWheels(float turnAngle)
        {
            float targetTurnAngle = turnAngle * turnPercentage;
            currentTurnAngle = Mathf.Lerp(currentTurnAngle, targetTurnAngle, Time.deltaTime * turnSpeed);

            FLwheelPivot.localRotation = Quaternion.Euler(0, currentTurnAngle, 0);
            FRwheelPivot.localRotation = Quaternion.Euler(0, currentTurnAngle, 0);
            BLwheelPivot.localRotation = Quaternion.Euler(0, -currentTurnAngle, 0);
            BRwheelPivot.localRotation = Quaternion.Euler(0, -currentTurnAngle, 0);
        }

        private void ResetCarPosition()
        {
            transform.rotation = Quaternion.Euler(0, 0, 0);
            CanMove = true;
            healthController.Heal(100);
        }

        public void SetLocalPlayer()
        {
            FunctionalButtons.Instance.vehicle = this;
            FunctionalButtons.Instance.attack = attack;
            FunctionalButtons.Instance.ListenToHealthController(healthController);
            isLocalPlayer = true;
            gameObject.name = "Local Player";
            playerCam.gameObject.SetActive(true);
        }

        public void SetBot()
        {
            isLocalPlayer = true;
            gameObject.name = Random.Range(0, 10).ToString();
        }

        public void OnPhotonSerializeView(PhotonStream stream, PhotonMessageInfo info)
        {
            if (stream.IsWriting)
            {
                // Отправляем данные другим игрокам
                stream.SendNext(CanMove);
            }
            else
            {
                // Получаем данные от других игроков
                CanMove = (bool)stream.ReceiveNext();
            }
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
            if (IsGrounded && CanMove && GameController.Instance.isGameStarted)
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
            if (IsGrounded && CanMove && GameController.Instance.isGameStarted)
            {
                float forceMagnitude = 0f; // Инициализируем переменную для хранения величины силы

                // Используем джойстик для мобильных устройств
                forceMagnitude = accelerationInput * FinalStats.Acceleration;

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

        public override void OnDestroy()
        {
            triggerCallback.OnTriggerEntered += OnTriggerEntered;
            healthController.OnDeath -= OnDeath;
        }

        private void OnDrawGizmosSelected()
        {
            groundDetection.OnDrawGizmosSelected(this);
        }
    }
}