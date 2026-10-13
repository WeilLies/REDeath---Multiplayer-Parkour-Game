//using System.Collections.Generic;
//using FMODUnity;
//using UnityEngine;

//namespace PlayerSystem
//{
//    [System.Serializable]
//    public class StepSound
//    {
//        public EventReference stepSound;
//        public PhysicsMaterial physicMaterial;
//    }

//    public class FootStepSystem : MonoBehaviour
//    {
//        [Header("Components")]
//        [SerializeField] private PlayerController playerController;
//        [SerializeField] private Transform stepSoundPos;

//        [Header("Footstep Parameters")]
//        [SerializeField] private LayerMask layers;
//        [SerializeField] private float distanceToGround = 1f;
//        [SerializeField] private float sphereRadius = 1f;
//        [SerializeField] private float walkTimeToNextStep = 0.5f;
//        [SerializeField] private float runTimeToNextStep = 1f;

//        [Header("Step Sounds & Physics Material")]
//        [SerializeField] private List<StepSound> stepSounds = new List<StepSound>();

//        private const float VALUE_TO_STEP = 100f;
//        private float currentValueToStep;

//        private IAudioManager audioManager;

//        private void Awake()
//        {
//            audioManager = ServiceLocator.Get<IAudioManager>();
//        }

//        private void OnEnable()
//        {
//            playerController.OnPlayerFallDown += CheckLayerGround;
//        }

//        private void OnDisable()
//        {
//            playerController.OnPlayerFallDown -= CheckLayerGround;
//        }

//        private void Update()
//        {
//            if (playerController.IsCrouching || !playerController.IsGrounded) { return; }

//            float speedValue = Mathf.Clamp(Mathf.Abs(playerController.CurrentInputVector.y) + Mathf.Abs(playerController.CurrentInputVector.x), 0f, 1f);
//            currentValueToStep += playerController.IsSprinting ? speedValue * runTimeToNextStep * Time.deltaTime : speedValue * walkTimeToNextStep * Time.deltaTime;

//            if (currentValueToStep >= VALUE_TO_STEP) { CheckLayerGround(); }
//        }

//        private void CheckLayerGround()
//        {
//            if (Physics.SphereCast(transform.position, sphereRadius, transform.TransformDirection(Vector3.down), out RaycastHit hit, distanceToGround, layers, QueryTriggerInteraction.Ignore))
//            {
//                if (hit.collider != null)
//                {
//                    PhysicsMaterial physicMaterial = hit.collider.sharedMaterial;

//                    foreach(var stepSound in stepSounds) 
//                    { 
//                        if (stepSound.physicMaterial == physicMaterial) 
//                        {
//                            currentValueToStep = 0f;

//                            SoundStep(stepSound.stepSound, stepSoundPos.position);
//                            break;
//                        }
//                    }
//                }
//            }
//        }

//        private void SoundStep(EventReference clip, Vector3 worldPos)
//        {
//            audioManager.PlayOneShot(clip, worldPos);
//        }
//    }
//}
