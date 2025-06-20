// Copyright (c) 2023 homuler
//
// Use of this source code is governed by an MIT-style
// license that can be found in the LICENSE file or at
// https://opensource.org/licenses/MIT.

using System.Collections;
using System.Collections.Generic;
using Mediapipe.Tasks.Vision.HandLandmarker;
using Mediapipe.Tasks.Components.Containers;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.UI;

namespace Mediapipe.Unity.Sample.HandLandmarkDetection
{
    /// <summary>
    /// �ֲ�����������������
    /// �����ʼ��������MediaPipe�ֲ������������񣬴���ͼ�����ݲ�չʾ�����
    /// </summary>
    public class HandLandmarkerRunner : VisionTaskApiRunner<HandLandmarker>
    {
        [SerializeField] private HandLandmarkerResultAnnotationController _handLandmarkerResultAnnotationController; // ���ڿ��ӻ��ֲ������������Ŀ�����

        private Experimental.TextureFramePool _textureFramePool; // ����֡�أ����ڹ��������������Դ

        public readonly HandLandmarkDetectionConfig config = new HandLandmarkDetectionConfig(); // �ֲ�������������

        [SerializeField] private UnityEngine.UI.Image background; // ����ͼ��

        [SerializeField] private float handsCloseThreshold = 0.15f; // �ж�˫�ֺ�£�ľ�����ֵ

        [SerializeField] private ParticleSystem clapParticleEffect; // ����ʱ��������Ч
        [SerializeField] private ParticleSystem fistParticleEffect; // ��ȭʱ��������Ч
        [SerializeField] private ParticleSystem thumbsUpParticleEffect; // ��������ʱ��������Ч

        // ����µ�������Ч
        [SerializeField] private ParticleSystem numberOneParticleEffect; // ��"1"���Ƶ�������Ч
        [SerializeField] private ParticleSystem numberTwoParticleEffect; // ��"2"���Ƶ�������Ч
        [SerializeField] private ParticleSystem numberThreeParticleEffect; // ��"3"���Ƶ�������Ч

        [SerializeField] private float effectZPosition = 130f; // ������Ч��Z��̶�λ��
        [SerializeField] private Camera mainCamera; // ���ڽ���׼������ת��Ϊ������������

        // ���������Ч״̬
        private bool isPlayingClapEffect = false; // �Ƿ����ڲ��ź�����Ч
        private bool isPlayingFistEffect = false; // �Ƿ����ڲ�����ȭ��Ч
        private bool isPlayingThumbsUpEffect = false; // �Ƿ����ڲ��ŵ�����Ч

        // ����µ���Ч״̬����
        private bool isPlayingNumberOneEffect = false;
        private bool isPlayingNumberTwoEffect = false;
        private bool isPlayingNumberThreeEffect = false;

        // ��¼������Ч�ϴδ���ʱ�䣬��ֹƵ������
        private float lastClapEffectTime = 0f;
        private float lastFistEffectTime = 0f;
        private float lastThumbsUpEffectTime = 0f;

        // �����Ч����ʱ���¼
        private float lastNumberOneEffectTime = 0f;
        private float lastNumberTwoEffectTime = 0f;
        private float lastNumberThreeEffectTime = 0f;

        // ��Ч��С���ʱ�䣨�룩
        private const float MIN_EFFECT_INTERVAL = 2.0f;

        // ���������߳��ϴ����ֲ������
        private HandLandmarkerResult latestResult;
        private readonly object resultLock = new object();
        private bool hasNewResult = false;

        private void Awake()
        {
            // ���û��ָ������������ȡ�����е������
            if (mainCamera == null)
            {
                mainCamera = Camera.main;
                if (mainCamera == null)
                {
                    Debug.LogWarning("δ�ҵ������������Inspector���ֶ����������");
                }
            }

            // ��ʼ��������Ч
            if (clapParticleEffect == null)
            {
                // ���Բ��ҳ����еĺ���������Ч
                clapParticleEffect = GameObject.Find("ClapParticleEffect")?.GetComponent<ParticleSystem>();
                if (clapParticleEffect == null)
                {
                    Debug.LogWarning("δ�ҵ�����������Ч������Inspector���ֶ�����");
                }
            }

            if (fistParticleEffect == null)
            {
                // ���Բ��ҳ����е���ȭ������Ч
                fistParticleEffect = GameObject.Find("FistParticleEffect")?.GetComponent<ParticleSystem>();
                if (fistParticleEffect == null)
                {
                    Debug.LogWarning("δ�ҵ���ȭ������Ч������Inspector���ֶ�����");
                }
            }

            if (thumbsUpParticleEffect == null)
            {
                // ���Բ��ҳ����еĵ���������Ч
                thumbsUpParticleEffect = GameObject.Find("ThumbsUpParticleEffect")?.GetComponent<ParticleSystem>();
                if (thumbsUpParticleEffect == null)
                {
                    Debug.LogWarning("δ�ҵ�����������Ч������Inspector���ֶ�����");
                }
            }

            if (numberOneParticleEffect == null)
            {
                numberOneParticleEffect = GameObject.Find("NumberOneParticleEffect")?.GetComponent<ParticleSystem>();
                if (numberOneParticleEffect == null)
                {
                    Debug.LogWarning("δ�ҵ�����1������Ч������Inspector���ֶ�����");
                }
            }

            if (numberTwoParticleEffect == null)
            {
                numberTwoParticleEffect = GameObject.Find("NumberTwoParticleEffect")?.GetComponent<ParticleSystem>();
                if (numberTwoParticleEffect == null)
                {
                    Debug.LogWarning("δ�ҵ�����2������Ч������Inspector���ֶ�����");
                }
            }

            if (numberThreeParticleEffect == null)
            {
                numberThreeParticleEffect = GameObject.Find("NumberThreeParticleEffect")?.GetComponent<ParticleSystem>();
                if (numberThreeParticleEffect == null)
                {
                    Debug.LogWarning("δ�ҵ�����3������Ч������Inspector���ֶ�����");
                }
            }

            // ȷ������ϵͳ��ʼ״̬Ϊֹͣ
            if (clapParticleEffect != null)
            {
                clapParticleEffect.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
            }

            if (fistParticleEffect != null)
            {
                fistParticleEffect.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
            }

            if (thumbsUpParticleEffect != null)
            {
                thumbsUpParticleEffect.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
            }

            if (numberOneParticleEffect != null)
            {
                numberOneParticleEffect.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
            }

            if (numberTwoParticleEffect != null)
            {
                numberTwoParticleEffect.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
            }

            if (numberThreeParticleEffect != null)
            {
                numberThreeParticleEffect.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
            }
        }

        // Update�����߳������У����ڴ������µļ����
        private void Update()
        {
            // ����Ƿ����µļ����
            if (hasNewResult)
            {
                HandLandmarkerResult resultCopy;
                lock (resultLock)
                {
                    resultCopy = latestResult;
                    hasNewResult = false;
                }

                // �����߳��ϴ�����
                ProcessHandLandmarkerResult(resultCopy);
            }

            // ����������ϵͳ��״̬
            UpdateParticleSystemStates();
        }

        /// <summary>
        /// ��������ϵͳ״̬
        /// </summary>
        private void UpdateParticleSystemStates()
        {
            // ��������Ч�Ƿ�����ɲ���
            if (isPlayingClapEffect && clapParticleEffect != null && !clapParticleEffect.IsAlive(true))
            {
                // ��ȫֹͣ�������������
                clapParticleEffect.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
                isPlayingClapEffect = false;
            }

            // �����ȭ��Ч�Ƿ�����ɲ���
            if (isPlayingFistEffect && fistParticleEffect != null && !fistParticleEffect.IsAlive(true))
            {
                // ��ȫֹͣ�������������
                fistParticleEffect.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
                isPlayingFistEffect = false;
            }

            // ��������Ч�Ƿ�����ɲ��� - ����һ�������ɵ��ж�����
            if (isPlayingThumbsUpEffect && thumbsUpParticleEffect != null &&
                !thumbsUpParticleEffect.IsAlive(true) &&
                Time.time - lastThumbsUpEffectTime > 2.0f) // ȷ�����ٲ���2��
            {
                // ��ȫֹͣ�������������
                thumbsUpParticleEffect.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
                isPlayingThumbsUpEffect = false;
            }

            // �������"1"��Ч״̬
            if (isPlayingNumberOneEffect && numberOneParticleEffect != null && !numberOneParticleEffect.IsAlive(true))
            {
                numberOneParticleEffect.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
                isPlayingNumberOneEffect = false;
            }

            // �������"2"��Ч״̬
            if (isPlayingNumberTwoEffect && numberTwoParticleEffect != null && !numberTwoParticleEffect.IsAlive(true))
            {
                numberTwoParticleEffect.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
                isPlayingNumberTwoEffect = false;
            }

            // �������"3"��Ч״̬
            if (isPlayingNumberThreeEffect && numberThreeParticleEffect != null && !numberThreeParticleEffect.IsAlive(true))
            {
                numberThreeParticleEffect.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
                isPlayingNumberThreeEffect = false;
            }
        }

        /// <summary>
        /// ֹͣ���в��ͷ���Դ
        /// </summary>
        public override void Stop()
        {
            base.Stop();
            _textureFramePool?.Dispose(); // �ͷ�����֡����Դ
            _textureFramePool = null;
        }

        /// <summary>
        /// �����ֲ������������Э��
        /// </summary>
        protected override IEnumerator Run()
        {
            // ���������Ϣ������̨
            Debug.Log($"Ӳ������ί�� = {config.Delegate}"); // Ӳ������ί��(CPU/GPU/NPU��)
            Debug.Log($"ͼ���ȡģʽ = {config.ImageReadMode}"); // ͼ���ȡģʽ
            Debug.Log($"����ģʽ = {config.RunningMode}"); // ����ģʽ(��ͼ��/��Ƶ/ʵʱ��)
            Debug.Log($"����������� = {config.NumHands}"); // �����������
            Debug.Log($"�ֲ������С���Ŷ� = {config.MinHandDetectionConfidence}"); // �ֲ������С���Ŷ�
            Debug.Log($"�ֲ�������С���Ŷ� = {config.MinHandPresenceConfidence}"); // �ֲ�������С���Ŷ�
            Debug.Log($"�ֲ�������С���Ŷ� = {config.MinTrackingConfidence}"); // �ֲ�������С���Ŷ�

            // �첽����ģ����Դ
            yield return AssetLoader.PrepareAssetAsync(config.ModelPath);

            // ����HandLandmarkerѡ������ʵʱ��ģʽ�����ûص�����
            var options = config.GetHandLandmarkerOptions(config.RunningMode == Tasks.Vision.Core.RunningMode.LIVE_STREAM ? OnHandLandmarkDetectionOutput : null);
            taskApi = HandLandmarker.CreateFromOptions(options, GpuManager.GpuResources); // ����HandLandmarkerʵ��
            var imageSource = ImageSourceProvider.ImageSource; // ��ȡͼ��Դ

            yield return imageSource.Play(); // ����ͼ��Դ

            // ���ͼ��Դ�Ƿ�׼������
            if (!imageSource.isPrepared)
            {
                Debug.LogError("Failed to start ImageSource, exiting...");
                yield break;
            }

            // ����RGBA32��ʽ������֡�أ����ڴ���ͼ������
            _textureFramePool = new Experimental.TextureFramePool(imageSource.textureWidth, imageSource.textureHeight, TextureFormat.RGBA32, 10);

            // ��ʼ����Ļ��ʾ�����ֿ�߱�
            screen.Initialize(imageSource);

            // ������������´���ʹ����ͷ���治�ɼ�
            screen.gameObject.GetComponent<RawImage>().enabled = false; // ����RawImage���

            //���һ����ɫ����
            if (background != null)
            {
                // ����и���Image���������������ɫ�������ɫ��͸����
                background.color = new UnityEngine.Color(0, 0, 0, 0); // ��ȫ͸������
            }

            // ����ע�Ϳ������Կ��ӻ���������������У�ȷ���ֲ��Ǽܿɼ���
            SetupAnnotationController(_handLandmarkerResultAnnotationController, imageSource);

            // ��ȡͼ��任ѡ��
            var transformationOptions = imageSource.GetTransformationOptions();
            var flipHorizontally = transformationOptions.flipHorizontally; // ˮƽ��ת
            var flipVertically = transformationOptions.flipVertically; // ��ֱ��ת
            var imageProcessingOptions = new Tasks.Vision.Core.ImageProcessingOptions(rotationDegrees: (int)transformationOptions.rotationAngle); // ͼ����ѡ��

            AsyncGPUReadbackRequest req = default; // GPU�첽��ȡ����
            var waitUntilReqDone = new WaitUntil(() => req.done); // �ȴ�GPU��ȡ���
            var waitForEndOfFrame = new WaitForEndOfFrame(); // �ȴ�֡����
            var result = HandLandmarkerResult.Alloc(options.numHands); // ����������

            // ע�����ǿ�����MediaPipe������Ⱦ�̵߳�GL������(Ŀǰ��֧��Android)
            var canUseGpuImage = SystemInfo.graphicsDeviceType == GraphicsDeviceType.OpenGLES3 && GpuManager.GpuResources != null;
            using var glContext = canUseGpuImage ? GpuManager.GetGlContext() : null;

            while (true)
            {
                // �����ͣ���ȴ��ָ�
                if (isPaused)
                {
                    yield return new WaitWhile(() => isPaused);
                }

                // ���Դ�����֡�ػ�ȡһ����������֡
                if (!_textureFramePool.TryGetTextureFrame(out var textureFrame))
                {
                    yield return new WaitForEndOfFrame();
                    continue;
                }

                // ��������ͼ��
                Image image;
                switch (config.ImageReadMode)
                {
                    case ImageReadMode.GPU: // GPUģʽ��ȡͼ��
                        if (!canUseGpuImage)
                        {
                            throw new System.Exception("ImageReadMode.GPU is not supported");
                        }
                        textureFrame.ReadTextureOnGPU(imageSource.GetCurrentTexture(), flipHorizontally, flipVertically); // ��GPU�϶�ȡ����
                        image = textureFrame.BuildGPUImage(glContext); // ����GPUͼ��
                                                                       // ע��Ŀǰ����������ȴ�һ֡��ȷ����������ȫ���Ƶ�TextureFrame��ŷ��͸�MediaPipe��
                                                                       // ���ַ���ͨ����Ч������֤����Ҫ�ҵ������ʵķ������μ�: https://github.com/homuler/MediaPipeUnityPlugin/pull/1311
                        yield return waitForEndOfFrame;
                        break;
                    case ImageReadMode.CPU: // CPUģʽ��ȡͼ��
                        yield return waitForEndOfFrame;
                        textureFrame.ReadTextureOnCPU(imageSource.GetCurrentTexture(), flipHorizontally, flipVertically); // ��CPU�϶�ȡ����
                        image = textureFrame.BuildCPUImage(); // ����CPUͼ��
                        textureFrame.Release(); // �ͷ�����֡
                        break;
                    case ImageReadMode.CPUAsync: // CPU�첽ģʽ��ȡͼ��(Ĭ��)
                    default:
                        req = textureFrame.ReadTextureAsync(imageSource.GetCurrentTexture(), flipHorizontally, flipVertically); // �첽��ȡ����
                        yield return waitUntilReqDone; // �ȴ���ȡ���

                        if (req.hasError) // ����ȡ����
                        {
                            Debug.LogWarning($"Failed to read texture from the image source");
                            continue;
                        }
                        image = textureFrame.BuildCPUImage(); // ����CPUͼ��
                        textureFrame.Release(); // �ͷ�����֡
                        break;
                }

                // ���ݲ�ͬ����ģʽ����ͼ��
                switch (taskApi.runningMode)
                {
                    case Tasks.Vision.Core.RunningMode.IMAGE: // ��ͼ��ģʽ
                        if (taskApi.TryDetect(image, imageProcessingOptions, ref result)) // ���Լ���ֲ�������
                        {
                            _handLandmarkerResultAnnotationController.DrawNow(result); // �������ƽ��

                            // �����߳���ֱ�Ӵ�����
                            ProcessHandLandmarkerResult(result);
                        }
                        else
                        {
                            _handLandmarkerResultAnnotationController.DrawNow(default); // û�м�⵽���������Ĭ��״̬
                        }
                        break;
                    case Tasks.Vision.Core.RunningMode.VIDEO: // ��Ƶģʽ
                        if (taskApi.TryDetectForVideo(image, GetCurrentTimestampMillisec(), imageProcessingOptions, ref result)) // ����Ϊ��Ƶ֡����ֲ�������
                        {
                            _handLandmarkerResultAnnotationController.DrawNow(result); // �������ƽ��

                            // �����߳���ֱ�Ӵ�����
                            ProcessHandLandmarkerResult(result);
                        }
                        else
                        {
                            _handLandmarkerResultAnnotationController.DrawNow(default); // û�м�⵽���������Ĭ��״̬
                        }
                        break;
                    case Tasks.Vision.Core.RunningMode.LIVE_STREAM: // ʵʱ��ģʽ
                        taskApi.DetectAsync(image, GetCurrentTimestampMillisec(), imageProcessingOptions); // �첽��⣬���ͨ���ص���������
                        break;
                }
            }
        }

        /// <summary>
        /// ��Tasks�����NormalizedLandmarkת��ΪMediapipe��NormalizedLandmark
        /// </summary>
        private Mediapipe.NormalizedLandmark ConvertToMediapipeLandmark(Tasks.Components.Containers.NormalizedLandmark landmark)
        {
            // ����һ���µ�Mediapipe.NormalizedLandmark����
            var proto = new Mediapipe.NormalizedLandmark();

            // ͨ����������x, y, z����
            var type = proto.GetType();
            type.GetProperty("X").SetValue(proto, landmark.x);
            type.GetProperty("Y").SetValue(proto, landmark.y);
            type.GetProperty("Z").SetValue(proto, landmark.z);

            return proto;
        }

        /// <summary>
        /// ����Ƿ���ȭ
        /// </summary>  
        private bool IsFist(NormalizedLandmarks landmarks)
        {
            // ���ʳָ����ָ������ָ��Сָ�Ƿ�����
            // ͨ���Ƚ�ָ���ָ����λ�����ж�
            bool isIndexFingerBent = landmarks.landmarks[8].y > landmarks.landmarks[6].y; // ʳָ
            bool isMiddleFingerBent = landmarks.landmarks[12].y > landmarks.landmarks[10].y; // ��ָ
            bool isRingFingerBent = landmarks.landmarks[16].y > landmarks.landmarks[14].y; // ����ָ
            bool isPinkyBent = landmarks.landmarks[20].y > landmarks.landmarks[18].y; // Сָ

            // ���Ĵָ�Ƿ�Ҳ���� (������ȭ����޵Ĺؼ�����)
            bool isThumbBent = landmarks.landmarks[4].y > landmarks.landmarks[2].y;

            // ���Ĵָ�Ƿ��������ڲ� (��ȭʱĴָͨ����������)
            bool isThumbInside = landmarks.landmarks[4].x > landmarks.landmarks[9].x;

            // ���������ָ����������Ĵָ��������״̬������Ϊ����ȭ
            return isIndexFingerBent && isMiddleFingerBent && isRingFingerBent && isPinkyBent &&
                   (isThumbBent || isThumbInside);
        }

        /// <summary>
        /// ����Ƿ�Ϊ�������ƣ�Ĵָ���ϣ�������ָ������
        /// </summary>
        private bool IsThumbsUp(NormalizedLandmarks landmarks)
        {
            // ���Ĵָ�Ƿ�����
            bool isThumbUp = landmarks.landmarks[4].y < landmarks.landmarks[3].y &&
                             landmarks.landmarks[3].y < landmarks.landmarks[2].y;

            // ���������ָ�Ƿ�����
            bool isIndexFingerBent = landmarks.landmarks[8].y > landmarks.landmarks[6].y; // ʳָ
            bool isMiddleFingerBent = landmarks.landmarks[12].y > landmarks.landmarks[10].y; // ��ָ
            bool isRingFingerBent = landmarks.landmarks[16].y > landmarks.landmarks[14].y; // ����ָ
            bool isPinkyBent = landmarks.landmarks[20].y > landmarks.landmarks[18].y; // Сָ

            // ���Ĵָ������������ָ����������Ϊ�ǵ�������
            return isThumbUp && isIndexFingerBent && isMiddleFingerBent && isRingFingerBent && isPinkyBent;
        }

        /// <summary>
        /// ����Ƿ�Ϊ����"1"���ƣ�ʳָ��ֱ��������ָ������
        /// </summary>
        private bool IsNumberOne(NormalizedLandmarks landmarks)
        {
            // ���ʳָ�Ƿ���ֱ
            bool isIndexFingerStraight = landmarks.landmarks[8].y < landmarks.landmarks[6].y;

            // ���������ָ�Ƿ�����
            bool isMiddleFingerBent = landmarks.landmarks[12].y > landmarks.landmarks[10].y;
            bool isRingFingerBent = landmarks.landmarks[16].y > landmarks.landmarks[14].y;
            bool isPinkyBent = landmarks.landmarks[20].y > landmarks.landmarks[18].y;
            bool isThumbBent = landmarks.landmarks[4].x < landmarks.landmarks[3].x;

            // ���ʳָ��ֱ��������ָ����������Ϊ��"1"����
            return isIndexFingerStraight && isMiddleFingerBent && isRingFingerBent && isPinkyBent && isThumbBent;
        }

        /// <summary>
        /// ����Ƿ�Ϊ����"2"���ƣ�ʳָ����ָ��ֱ��������ָ������
        /// </summary>
        private bool IsNumberTwo(NormalizedLandmarks landmarks)
        {
            // ���ʳָ����ָ�Ƿ���ֱ
            bool isIndexFingerStraight = landmarks.landmarks[8].y < landmarks.landmarks[6].y;
            bool isMiddleFingerStraight = landmarks.landmarks[12].y < landmarks.landmarks[10].y;

            // ���������ָ�Ƿ�����
            bool isRingFingerBent = landmarks.landmarks[16].y > landmarks.landmarks[14].y;
            bool isPinkyBent = landmarks.landmarks[20].y > landmarks.landmarks[18].y;
            bool isThumbBent = landmarks.landmarks[4].x < landmarks.landmarks[3].x;

            // ���ʳָ����ָ��ֱ��������ָ����������Ϊ��"2"����
            return isIndexFingerStraight && isMiddleFingerStraight && isRingFingerBent && isPinkyBent && isThumbBent;
        }

        /// <summary>
        /// ����Ƿ�Ϊ����"3"���ƣ�ʳָ����ָ������ָ��ֱ��������ָ������
        /// </summary>
        private bool IsNumberThree(NormalizedLandmarks landmarks)
        {
            // ���ʳָ����ָ������ָ�Ƿ���ֱ
            bool isIndexFingerStraight = landmarks.landmarks[8].y < landmarks.landmarks[6].y;
            bool isMiddleFingerStraight = landmarks.landmarks[12].y < landmarks.landmarks[10].y;
            bool isRingFingerStraight = landmarks.landmarks[16].y < landmarks.landmarks[14].y;

            // ���������ָ�Ƿ�����
            bool isPinkyBent = landmarks.landmarks[20].y > landmarks.landmarks[18].y;
            bool isThumbBent = landmarks.landmarks[4].x < landmarks.landmarks[3].x;

            // ���ʳָ����ָ������ָ��ֱ��������ָ����������Ϊ��"3"����
            return isIndexFingerStraight && isMiddleFingerStraight && isRingFingerStraight && isPinkyBent && isThumbBent;
        }

        /// <summary>
        /// �ж��Ƿ�Ϊ����
        /// </summary>
        private bool IsLeftHand(Classifications handedness)
        {
            // MediaPipe���ֲ�����ͨ������"Left"��"Right"���
            // ����������ͷ����Ч����ʵ���Ͽ������෴��
            // ����һ������ı�ǩ�Ƿ�Ϊ"Left"
            if (handedness.categories.Count > 0)
            {
                return handedness.categories[0].categoryName.Contains("Left");
            }
            return false;
        }

        /// <summary>
        /// �ж��Ƿ�Ϊ����
        /// </summary>
        private bool IsRightHand(Classifications handedness)
        {
            // ����һ������ı�ǩ�Ƿ�Ϊ"Right"
            if (handedness.categories.Count > 0)
            {
                return handedness.categories[0].categoryName.Contains("Right");
            }
            return false;
        }

        /// <summary>
        /// ���ֲ���׼������ת��Ϊ��������
        /// </summary>
        /// <param name="landmark">��׼�����ֲ�����</param>
        /// <returns>��Ӧ����������</returns>
        private Vector3 HandPositionToWorldPosition(Tasks.Components.Containers.NormalizedLandmark landmark)
        {
            if (mainCamera == null) return Vector3.zero;

            // ��׼�������(0,0)����ͼ�����Ͻǣ�(1,1)�����½�
            // ��Ҫ��y���귭ת����ΪUnity����Ļ����ϵy�ᳯ�ϣ���ͼ������ϵy�ᳯ��
            float screenX = landmark.x * UnityEngine.Screen.width;
            float screenY = (1 - landmark.y) * UnityEngine.Screen.height; // ��תY����

            // ����Ļ����ת��Ϊ��������
            Vector3 worldPos = mainCamera.ScreenToWorldPoint(new Vector3(screenX, screenY, effectZPosition));
            return worldPos;
        }

        /// <summary>
        /// ������ֻ�ּ���е�λ�ã�������Ϊ��׼�㣩
        /// </summary>
        private Vector3 CalculateHandsMidPoint(NormalizedLandmarks hand1, NormalizedLandmarks hand2)
        {
            if (mainCamera == null) return Vector3.zero;

            // ��ȡ��ֻ�ֵ�����λ�ã�����0������
            var wrist1 = hand1.landmarks[0];
            var wrist2 = hand2.landmarks[0];

            // ������������Ļ�ϵ�����
            float screen1X = wrist1.x * UnityEngine.Screen.width;
            float screen1Y = (1 - wrist1.y) * UnityEngine.Screen.height;

            float screen2X = wrist2.x * UnityEngine.Screen.width;
            float screen2Y = (1 - wrist2.y) * UnityEngine.Screen.height;

            // �����е����Ļ����
            float midScreenX = (screen1X + screen2X) / 2;
            float midScreenY = (screen1Y + screen2Y) / 2;

            // ת��Ϊ��������
            Vector3 worldPos = mainCamera.ScreenToWorldPoint(new Vector3(midScreenX, midScreenY, effectZPosition));
            return worldPos;
        }

        /// <summary>
        /// �����߳��ϴ����ֲ�������Ϳ���������Ч
        /// </summary>
        private void ProcessHandLandmarkerResult(HandLandmarkerResult result)
        {
            // ���û�м�⵽�ֻ����ƣ���ֹͣ������Ч
            if (result.handLandmarks == null || result.handLandmarks.Count == 0 || result.handedness == null)
            {
                // �����ǰ���κ���Ч�ڲ��ţ���ֹͣ����
                if (isPlayingClapEffect || isPlayingFistEffect || isPlayingThumbsUpEffect ||
                    isPlayingNumberOneEffect || isPlayingNumberTwoEffect || isPlayingNumberThreeEffect)
                {
                    StopAllParticleEffects();
                }
                return;
            }

            // ���������ֵ�����
            int leftHandIndex = -1;
            int rightHandIndex = -1;

            // ʶ��������
            for (int i = 0; i < result.handedness.Count; i++)
            {
                if (IsLeftHand(result.handedness[i]))
                {
                    leftHandIndex = i;
                }
                else if (IsRightHand(result.handedness[i]))
                {
                    rightHandIndex = i;
                }
            }

            // ������ֵ�������
            bool isRightHandThumbsUp = false;
            if (rightHandIndex >= 0 && rightHandIndex < result.handLandmarks.Count)
            {
                isRightHandThumbsUp = IsThumbsUp(result.handLandmarks[rightHandIndex]);
            }

            // ���������ȭ����
            bool isLeftHandFist = false;
            if (leftHandIndex >= 0 && leftHandIndex < result.handLandmarks.Count)
            {
                isLeftHandFist = IsFist(result.handLandmarks[leftHandIndex]);
            }

            // ���������������
            bool isLeftHandNumberOne = false;
            bool isLeftHandNumberTwo = false;
            bool isLeftHandNumberThree = false;

            if (leftHandIndex >= 0 && leftHandIndex < result.handLandmarks.Count)
            {
                isLeftHandNumberOne = IsNumberOne(result.handLandmarks[leftHandIndex]);
                isLeftHandNumberTwo = IsNumberTwo(result.handLandmarks[leftHandIndex]);
                isLeftHandNumberThree = IsNumberThree(result.handLandmarks[leftHandIndex]);
            }

            // ����������������ƣ����ŵ�����Ч
            if (isRightHandThumbsUp && !isPlayingThumbsUpEffect &&
                Time.time - lastThumbsUpEffectTime >= MIN_EFFECT_INTERVAL)
            {
                // ��ȡ����λ��
                Vector3 handPosition = HandPositionToWorldPosition(result.handLandmarks[rightHandIndex].landmarks[0]);

                PlayThumbsUpParticleEffect(handPosition);
                lastThumbsUpEffectTime = Time.time;
                Debug.Log("��⵽���ֵ������ƣ����ŵ���������Ч!");
                return; // �Ѵ���������ƣ������������Ƽ��
            }
            else if (!isRightHandThumbsUp && isPlayingThumbsUpEffect)
            {
                // ������ټ�⵽�������Ƶ���Ч���ڲ��ţ���ֹͣ��Ч
                StopThumbsUpParticleEffect();
            }

            // ���������ȭ��������ȭ��Ч
            if (isLeftHandFist && !isPlayingFistEffect &&
                Time.time - lastFistEffectTime >= MIN_EFFECT_INTERVAL)
            {
                // ��ȡ����λ��
                Vector3 handPosition = HandPositionToWorldPosition(result.handLandmarks[leftHandIndex].landmarks[0]);

                PlayFistParticleEffect(handPosition);
                lastFistEffectTime = Time.time;
                Debug.Log("��⵽������ȭ��������ȭ������Ч!");
                return; // �Ѵ�����ȭ���ƣ������������Ƽ��
            }
            else if (!isLeftHandFist && isPlayingFistEffect)
            {
                // ������ټ�⵽��ȭ���Ƶ���Ч���ڲ��ţ���ֹͣ��Ч
                StopFistParticleEffect();
            }

            // ������ֱ�"1"����
            if (isLeftHandNumberOne && !isPlayingNumberOneEffect &&
                Time.time - lastNumberOneEffectTime >= MIN_EFFECT_INTERVAL)
            {
                Vector3 handPosition = HandPositionToWorldPosition(result.handLandmarks[leftHandIndex].landmarks[0]);
                PlayNumberOneParticleEffect(handPosition);
                lastNumberOneEffectTime = Time.time;
                Debug.Log("��⵽���ֱ�'1'���ƣ���������1������Ч!");
                return; // �Ѵ������ƣ������������Ƽ��
            }
            else if (!isLeftHandNumberOne && isPlayingNumberOneEffect)
            {
                StopNumberOneParticleEffect();
            }

            // ������ֱ�"2"����
            if (isLeftHandNumberTwo && !isPlayingNumberTwoEffect &&
                Time.time - lastNumberTwoEffectTime >= MIN_EFFECT_INTERVAL)
            {
                Vector3 handPosition = HandPositionToWorldPosition(result.handLandmarks[leftHandIndex].landmarks[0]);
                PlayNumberTwoParticleEffect(handPosition);
                lastNumberTwoEffectTime = Time.time;
                Debug.Log("��⵽���ֱ�'2'���ƣ���������2������Ч!");
                return; // �Ѵ������ƣ������������Ƽ��
            }
            else if (!isLeftHandNumberTwo && isPlayingNumberTwoEffect)
            {
                StopNumberTwoParticleEffect();
            }

            // ������ֱ�"3"����
            if (isLeftHandNumberThree && !isPlayingNumberThreeEffect &&
                Time.time - lastNumberThreeEffectTime >= MIN_EFFECT_INTERVAL)
            {
                Vector3 handPosition = HandPositionToWorldPosition(result.handLandmarks[leftHandIndex].landmarks[0]);
                PlayNumberThreeParticleEffect(handPosition);
                lastNumberThreeEffectTime = Time.time;
                Debug.Log("��⵽���ֱ�'3'���ƣ���������3������Ч!");
                return; // �Ѵ������ƣ������������Ƽ��
            }
            else if (!isLeftHandNumberThree && isPlayingNumberThreeEffect)
            {
                StopNumberThreeParticleEffect();
            }

            // ����Ƿ�˫�ֶ�����⵽�����ں��Ƽ��
            if (leftHandIndex >= 0 && rightHandIndex >= 0)
            {
                // ��������֮����е�λ��
                Vector3 midWorldPos = CalculateHandsMidPoint(result.handLandmarks[leftHandIndex], result.handLandmarks[rightHandIndex]);

                // ��ֻ�ֵ�����λ�ã����ڼ������
                var leftHandWrist = result.handLandmarks[leftHandIndex].landmarks[0];
                var rightHandWrist = result.handLandmarks[rightHandIndex].landmarks[0];

                // ������������֮��ľ���
                float distance = Vector3.Distance(
                    new Vector3(leftHandWrist.x, leftHandWrist.y, leftHandWrist.z),
                    new Vector3(rightHandWrist.x, rightHandWrist.y, rightHandWrist.z)
                );

                // �������С����ֵ������Ϊ˫�ֺ�£
                if (distance < handsCloseThreshold && !isPlayingClapEffect &&
                         Time.time - lastClapEffectTime >= MIN_EFFECT_INTERVAL)
                {
                    // ���ź���������Ч��������֮����е�λ�ã�
                    PlayClapParticleEffect(midWorldPos);
                    lastClapEffectTime = Time.time;
                    Debug.Log("��⵽˫�ֺ�£�����ź���������Ч! ����: " + distance);
                }
                else if (distance >= handsCloseThreshold && isPlayingClapEffect)
                {
                    // ������ټ�⵽�������Ƶ���Ч���ڲ��ţ���ֹͣ��Ч
                    StopClapParticleEffect();
                }
            }
            else if (isPlayingClapEffect)
            {
                // ���������Ч���ڲ��ŵ�����û��˫�֣���ֹͣ��Ч
                StopClapParticleEffect();
            }
        }

        /// <summary>
        /// ֹͣ����������Ч
        /// </summary>
        private void StopAllParticleEffects()
        {
            // ֻ�е����Ե���Ч���ڲ���ʱ��ֹͣ����
            if (isPlayingClapEffect)
            {
                clapParticleEffect.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
                isPlayingClapEffect = false;
            }

            if (isPlayingFistEffect)
            {
                fistParticleEffect.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
                isPlayingFistEffect = false;
            }

            if (isPlayingThumbsUpEffect)
            {
                thumbsUpParticleEffect.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
                isPlayingThumbsUpEffect = false;
            }

            // ��Ӷ�����Ч�Ĵ���
            if (isPlayingNumberOneEffect)
            {
                numberOneParticleEffect.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
                isPlayingNumberOneEffect = false;
            }

            if (isPlayingNumberTwoEffect)
            {
                numberTwoParticleEffect.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
                isPlayingNumberTwoEffect = false;
            }

            if (isPlayingNumberThreeEffect)
            {
                numberThreeParticleEffect.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
                isPlayingNumberThreeEffect = false;
            }
        }

        /// <summary>
        /// ���ź���������Ч
        /// </summary>
        private void PlayClapParticleEffect(Vector3 position)
        {
            if (clapParticleEffect != null && !isPlayingClapEffect)
            {
                // ֹͣ������Ч
                StopFistParticleEffect();
                StopThumbsUpParticleEffect();

                // ��������ϵͳλ��
                clapParticleEffect.transform.position = position;

                // ��ȷ����ȫ���
                clapParticleEffect.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
                // ���ź�����Ч
                clapParticleEffect.Play();
                isPlayingClapEffect = true;
            }
        }

        /// <summary>
        /// ֹͣ����������Ч
        /// </summary>
        private void StopClapParticleEffect()
        {
            if (clapParticleEffect != null && isPlayingClapEffect)
            {
                // ֹͣ���������ӣ��������������������������
                clapParticleEffect.Stop(true, ParticleSystemStopBehavior.StopEmitting);
                // ״̬��ǻ���Update�м������ϵͳ�Ƿ��л�Ծ����
            }
        }

        /// <summary>
        /// ������ȭ������Ч
        /// </summary>
        private void PlayFistParticleEffect(Vector3 position)
        {
            if (fistParticleEffect != null && !isPlayingFistEffect)
            {
                // ֹͣ������Ч
                StopClapParticleEffect();
                StopThumbsUpParticleEffect();

                // ��������ϵͳλ��
                fistParticleEffect.transform.position = position;

                // ��ȷ����ȫ���
                fistParticleEffect.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
                // ������ȭ��Ч
                fistParticleEffect.Play();
                isPlayingFistEffect = true;
            }
        }

        /// <summary>
        /// ֹͣ��ȭ������Ч
        /// </summary>
        private void StopFistParticleEffect()
        {
            if (fistParticleEffect != null && isPlayingFistEffect)
            {
                // ֹͣ���������ӣ��������������������������
                fistParticleEffect.Stop(true, ParticleSystemStopBehavior.StopEmitting);
                // ״̬��ǻ���Update�м������ϵͳ�Ƿ��л�Ծ����
            }
        }

        /// <summary>
        /// ���ŵ���������Ч
        /// </summary>
        private void PlayThumbsUpParticleEffect(Vector3 position)
        {
            if (thumbsUpParticleEffect != null && !isPlayingThumbsUpEffect)
            {
                // ֹͣ������Ч
                StopClapParticleEffect();
                StopFistParticleEffect();

                // ��������ϵͳλ��
                thumbsUpParticleEffect.transform.position = position;

                // ��ȷ����ȫ���
                thumbsUpParticleEffect.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
                // ���ŵ�����Ч
                thumbsUpParticleEffect.Play();
                isPlayingThumbsUpEffect = true;
            }
        }

        /// <summary>
        /// ֹͣ����������Ч
        /// </summary>
        private void StopThumbsUpParticleEffect()
        {
            if (thumbsUpParticleEffect != null && isPlayingThumbsUpEffect)
            {
                // ֹͣ���������ӣ��������������������������
                thumbsUpParticleEffect.Stop(true, ParticleSystemStopBehavior.StopEmitting);
                // ״̬��ǻ���Update�м������ϵͳ�Ƿ��л�Ծ����
            }
        }

        /// <summary>
        /// ��������"1"������Ч
        /// </summary>
        private void PlayNumberOneParticleEffect(Vector3 position)
        {
            if (numberOneParticleEffect != null && !isPlayingNumberOneEffect)
            {
                // ֹͣ������Ч
                StopAllParticleEffects();

                // ��������ϵͳλ��
                numberOneParticleEffect.transform.position = position;

                // ��ȷ����ȫ���
                numberOneParticleEffect.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
                // ������Ч
                numberOneParticleEffect.Play();
                isPlayingNumberOneEffect = true;
            }
        }

        /// <summary>
        /// ֹͣ����"1"������Ч
        /// </summary>
        private void StopNumberOneParticleEffect()
        {
            if (numberOneParticleEffect != null && isPlayingNumberOneEffect)
            {
                numberOneParticleEffect.Stop(true, ParticleSystemStopBehavior.StopEmitting);
            }
        }

        /// <summary>
        /// ��������"2"������Ч
        /// </summary>
        private void PlayNumberTwoParticleEffect(Vector3 position)
        {
            if (numberTwoParticleEffect != null && !isPlayingNumberTwoEffect)
            {
                // ֹͣ������Ч
                StopAllParticleEffects();

                // ��������ϵͳλ��
                numberTwoParticleEffect.transform.position = position;

                // ��ȷ����ȫ���
                numberTwoParticleEffect.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
                // ������Ч
                numberTwoParticleEffect.Play();
                isPlayingNumberTwoEffect = true;
            }
        }

        /// <summary>
        /// ֹͣ����"2"������Ч
        /// </summary>
        private void StopNumberTwoParticleEffect()
        {
            if (numberTwoParticleEffect != null && isPlayingNumberTwoEffect)
            {
                numberTwoParticleEffect.Stop(true, ParticleSystemStopBehavior.StopEmitting);
            }
        }

        /// <summary>
        /// ��������"3"������Ч
        /// </summary>
        private void PlayNumberThreeParticleEffect(Vector3 position)
        {
            if (numberThreeParticleEffect != null && !isPlayingNumberThreeEffect)
            {
                // ֹͣ������Ч
                StopAllParticleEffects();

                // ��ȷ����ȫ���
                numberThreeParticleEffect.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
                // ������Ч
                numberThreeParticleEffect.Play();
                isPlayingNumberThreeEffect = true;
            }
        }

        /// <summary>
        /// ֹͣ����"3"������Ч
        /// </summary>
        private void StopNumberThreeParticleEffect()
        {
            if (numberThreeParticleEffect != null && isPlayingNumberThreeEffect)
            {
                numberThreeParticleEffect.Stop(true, ParticleSystemStopBehavior.StopEmitting);
            }
        }

        /// <summary>
        /// ʵʱ��ģʽ�µļ�����ص�����
        /// </summary>
        /// <param name="result">�ֲ�����������</param>
        /// <param name="image">����ͼ��</param>
        /// <param name="timestamp">ʱ���</param>
        private void OnHandLandmarkDetectionOutput(HandLandmarkerResult result, Image image, long timestamp)
        {
            _handLandmarkerResultAnnotationController.DrawLater(result); // �Ժ���ƽ��������һ֡��Ⱦʱ��

            // ��Ϊ����ص������ں�̨�̣߳�������Ҫ��������ݵ����߳�
            // �����½������������Ȼ����Update�����д���
            lock (resultLock)
            {
                latestResult = result;
                hasNewResult = true;
            }
        }
    }
}