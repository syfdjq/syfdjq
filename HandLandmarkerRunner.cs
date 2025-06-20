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
    /// 手部特征点检测运行器类
    /// 负责初始化和运行MediaPipe手部特征点检测任务，处理图像数据并展示检测结果
    /// </summary>
    public class HandLandmarkerRunner : VisionTaskApiRunner<HandLandmarker>
    {
        [SerializeField] private HandLandmarkerResultAnnotationController _handLandmarkerResultAnnotationController; // 用于可视化手部特征点检测结果的控制器

        private Experimental.TextureFramePool _textureFramePool; // 纹理帧池，用于管理和重用纹理资源

        public readonly HandLandmarkDetectionConfig config = new HandLandmarkDetectionConfig(); // 手部特征点检测配置

        [SerializeField] private UnityEngine.UI.Image background; // 背景图像

        [SerializeField] private float handsCloseThreshold = 0.15f; // 判断双手合拢的距离阈值

        [SerializeField] private ParticleSystem clapParticleEffect; // 合掌时的粒子特效
        [SerializeField] private ParticleSystem fistParticleEffect; // 握拳时的粒子特效
        [SerializeField] private ParticleSystem thumbsUpParticleEffect; // 点赞手势时的粒子特效

        // 添加新的粒子特效
        [SerializeField] private ParticleSystem numberOneParticleEffect; // 比"1"手势的粒子特效
        [SerializeField] private ParticleSystem numberTwoParticleEffect; // 比"2"手势的粒子特效
        [SerializeField] private ParticleSystem numberThreeParticleEffect; // 比"3"手势的粒子特效

        [SerializeField] private float effectZPosition = 130f; // 粒子特效的Z轴固定位置
        [SerializeField] private Camera mainCamera; // 用于将标准化坐标转换为世界坐标的相机

        // 标记粒子特效状态
        private bool isPlayingClapEffect = false; // 是否正在播放合掌特效
        private bool isPlayingFistEffect = false; // 是否正在播放握拳特效
        private bool isPlayingThumbsUpEffect = false; // 是否正在播放点赞特效

        // 添加新的特效状态跟踪
        private bool isPlayingNumberOneEffect = false;
        private bool isPlayingNumberTwoEffect = false;
        private bool isPlayingNumberThreeEffect = false;

        // 记录粒子特效上次触发时间，防止频繁触发
        private float lastClapEffectTime = 0f;
        private float lastFistEffectTime = 0f;
        private float lastThumbsUpEffectTime = 0f;

        // 添加特效触发时间记录
        private float lastNumberOneEffectTime = 0f;
        private float lastNumberTwoEffectTime = 0f;
        private float lastNumberThreeEffectTime = 0f;

        // 特效最小间隔时间（秒）
        private const float MIN_EFFECT_INTERVAL = 2.0f;

        // 用于在主线程上处理手部检测结果
        private HandLandmarkerResult latestResult;
        private readonly object resultLock = new object();
        private bool hasNewResult = false;

        private void Awake()
        {
            // 如果没有指定主相机，则获取场景中的主相机
            if (mainCamera == null)
            {
                mainCamera = Camera.main;
                if (mainCamera == null)
                {
                    Debug.LogWarning("未找到主相机，请在Inspector中手动分配主相机");
                }
            }

            // 初始化粒子特效
            if (clapParticleEffect == null)
            {
                // 尝试查找场景中的合掌粒子特效
                clapParticleEffect = GameObject.Find("ClapParticleEffect")?.GetComponent<ParticleSystem>();
                if (clapParticleEffect == null)
                {
                    Debug.LogWarning("未找到合掌粒子特效，请在Inspector中手动分配");
                }
            }

            if (fistParticleEffect == null)
            {
                // 尝试查找场景中的握拳粒子特效
                fistParticleEffect = GameObject.Find("FistParticleEffect")?.GetComponent<ParticleSystem>();
                if (fistParticleEffect == null)
                {
                    Debug.LogWarning("未找到握拳粒子特效，请在Inspector中手动分配");
                }
            }

            if (thumbsUpParticleEffect == null)
            {
                // 尝试查找场景中的点赞粒子特效
                thumbsUpParticleEffect = GameObject.Find("ThumbsUpParticleEffect")?.GetComponent<ParticleSystem>();
                if (thumbsUpParticleEffect == null)
                {
                    Debug.LogWarning("未找到点赞粒子特效，请在Inspector中手动分配");
                }
            }

            if (numberOneParticleEffect == null)
            {
                numberOneParticleEffect = GameObject.Find("NumberOneParticleEffect")?.GetComponent<ParticleSystem>();
                if (numberOneParticleEffect == null)
                {
                    Debug.LogWarning("未找到数字1粒子特效，请在Inspector中手动分配");
                }
            }

            if (numberTwoParticleEffect == null)
            {
                numberTwoParticleEffect = GameObject.Find("NumberTwoParticleEffect")?.GetComponent<ParticleSystem>();
                if (numberTwoParticleEffect == null)
                {
                    Debug.LogWarning("未找到数字2粒子特效，请在Inspector中手动分配");
                }
            }

            if (numberThreeParticleEffect == null)
            {
                numberThreeParticleEffect = GameObject.Find("NumberThreeParticleEffect")?.GetComponent<ParticleSystem>();
                if (numberThreeParticleEffect == null)
                {
                    Debug.LogWarning("未找到数字3粒子特效，请在Inspector中手动分配");
                }
            }

            // 确保粒子系统初始状态为停止
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

        // Update在主线程上运行，用于处理最新的检测结果
        private void Update()
        {
            // 检查是否有新的检测结果
            if (hasNewResult)
            {
                HandLandmarkerResult resultCopy;
                lock (resultLock)
                {
                    resultCopy = latestResult;
                    hasNewResult = false;
                }

                // 在主线程上处理结果
                ProcessHandLandmarkerResult(resultCopy);
            }

            // 检查各个粒子系统的状态
            UpdateParticleSystemStates();
        }

        /// <summary>
        /// 更新粒子系统状态
        /// </summary>
        private void UpdateParticleSystemStates()
        {
            // 检查合掌特效是否已完成播放
            if (isPlayingClapEffect && clapParticleEffect != null && !clapParticleEffect.IsAlive(true))
            {
                // 完全停止并清除所有粒子
                clapParticleEffect.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
                isPlayingClapEffect = false;
            }

            // 检查握拳特效是否已完成播放
            if (isPlayingFistEffect && fistParticleEffect != null && !fistParticleEffect.IsAlive(true))
            {
                // 完全停止并清除所有粒子
                fistParticleEffect.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
                isPlayingFistEffect = false;
            }

            // 检查点赞特效是否已完成播放 - 增加一个更宽松的判断条件
            if (isPlayingThumbsUpEffect && thumbsUpParticleEffect != null &&
                !thumbsUpParticleEffect.IsAlive(true) &&
                Time.time - lastThumbsUpEffectTime > 2.0f) // 确保至少播放2秒
            {
                // 完全停止并清除所有粒子
                thumbsUpParticleEffect.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
                isPlayingThumbsUpEffect = false;
            }

            // 检查数字"1"特效状态
            if (isPlayingNumberOneEffect && numberOneParticleEffect != null && !numberOneParticleEffect.IsAlive(true))
            {
                numberOneParticleEffect.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
                isPlayingNumberOneEffect = false;
            }

            // 检查数字"2"特效状态
            if (isPlayingNumberTwoEffect && numberTwoParticleEffect != null && !numberTwoParticleEffect.IsAlive(true))
            {
                numberTwoParticleEffect.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
                isPlayingNumberTwoEffect = false;
            }

            // 检查数字"3"特效状态
            if (isPlayingNumberThreeEffect && numberThreeParticleEffect != null && !numberThreeParticleEffect.IsAlive(true))
            {
                numberThreeParticleEffect.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
                isPlayingNumberThreeEffect = false;
            }
        }

        /// <summary>
        /// 停止运行并释放资源
        /// </summary>
        public override void Stop()
        {
            base.Stop();
            _textureFramePool?.Dispose(); // 释放纹理帧池资源
            _textureFramePool = null;
        }

        /// <summary>
        /// 运行手部特征点检测的主协程
        /// </summary>
        protected override IEnumerator Run()
        {
            // 输出配置信息到控制台
            Debug.Log($"硬件加速委托 = {config.Delegate}"); // 硬件加速委托(CPU/GPU/NPU等)
            Debug.Log($"图像读取模式 = {config.ImageReadMode}"); // 图像读取模式
            Debug.Log($"运行模式 = {config.RunningMode}"); // 运行模式(单图像/视频/实时流)
            Debug.Log($"最大检测手数量 = {config.NumHands}"); // 最大检测手数量
            Debug.Log($"手部检测最小置信度 = {config.MinHandDetectionConfidence}"); // 手部检测最小置信度
            Debug.Log($"手部存在最小置信度 = {config.MinHandPresenceConfidence}"); // 手部存在最小置信度
            Debug.Log($"手部跟踪最小置信度 = {config.MinTrackingConfidence}"); // 手部跟踪最小置信度

            // 异步加载模型资源
            yield return AssetLoader.PrepareAssetAsync(config.ModelPath);

            // 创建HandLandmarker选项，如果是实时流模式则设置回调函数
            var options = config.GetHandLandmarkerOptions(config.RunningMode == Tasks.Vision.Core.RunningMode.LIVE_STREAM ? OnHandLandmarkDetectionOutput : null);
            taskApi = HandLandmarker.CreateFromOptions(options, GpuManager.GpuResources); // 创建HandLandmarker实例
            var imageSource = ImageSourceProvider.ImageSource; // 获取图像源

            yield return imageSource.Play(); // 启动图像源

            // 检查图像源是否准备就绪
            if (!imageSource.isPrepared)
            {
                Debug.LogError("Failed to start ImageSource, exiting...");
                yield break;
            }

            // 创建RGBA32格式的纹理帧池，用于处理图像数据
            _textureFramePool = new Experimental.TextureFramePool(imageSource.textureWidth, imageSource.textureHeight, TextureFormat.RGBA32, 10);

            // 初始化屏幕显示，保持宽高比
            screen.Initialize(imageSource);

            // 在这里添加以下代码使摄像头画面不可见
            screen.gameObject.GetComponent<RawImage>().enabled = false; // 禁用RawImage组件

            //添加一个纯色背景
            if (background != null)
            {
                // 如果有父级Image组件，可以设置颜色（例如黑色或透明）
                background.color = new UnityEngine.Color(0, 0, 0, 0); // 完全透明背景
            }

            // 设置注释控制器以可视化检测结果（保留这行，确保手部骨架可见）
            SetupAnnotationController(_handLandmarkerResultAnnotationController, imageSource);

            // 获取图像变换选项
            var transformationOptions = imageSource.GetTransformationOptions();
            var flipHorizontally = transformationOptions.flipHorizontally; // 水平翻转
            var flipVertically = transformationOptions.flipVertically; // 垂直翻转
            var imageProcessingOptions = new Tasks.Vision.Core.ImageProcessingOptions(rotationDegrees: (int)transformationOptions.rotationAngle); // 图像处理选项

            AsyncGPUReadbackRequest req = default; // GPU异步读取请求
            var waitUntilReqDone = new WaitUntil(() => req.done); // 等待GPU读取完成
            var waitForEndOfFrame = new WaitForEndOfFrame(); // 等待帧结束
            var result = HandLandmarkerResult.Alloc(options.numHands); // 分配结果对象

            // 注：我们可以与MediaPipe共享渲染线程的GL上下文(目前仅支持Android)
            var canUseGpuImage = SystemInfo.graphicsDeviceType == GraphicsDeviceType.OpenGLES3 && GpuManager.GpuResources != null;
            using var glContext = canUseGpuImage ? GpuManager.GetGlContext() : null;

            while (true)
            {
                // 如果暂停，等待恢复
                if (isPaused)
                {
                    yield return new WaitWhile(() => isPaused);
                }

                // 尝试从纹理帧池获取一个可用纹理帧
                if (!_textureFramePool.TryGetTextureFrame(out var textureFrame))
                {
                    yield return new WaitForEndOfFrame();
                    continue;
                }

                // 构建输入图像
                Image image;
                switch (config.ImageReadMode)
                {
                    case ImageReadMode.GPU: // GPU模式读取图像
                        if (!canUseGpuImage)
                        {
                            throw new System.Exception("ImageReadMode.GPU is not supported");
                        }
                        textureFrame.ReadTextureOnGPU(imageSource.GetCurrentTexture(), flipHorizontally, flipVertically); // 在GPU上读取纹理
                        image = textureFrame.BuildGPUImage(glContext); // 构建GPU图像
                                                                       // 注：目前我们在这里等待一帧，确保纹理已完全复制到TextureFrame后才发送给MediaPipe。
                                                                       // 这种方法通常有效但不保证。需要找到更合适的方法。参见: https://github.com/homuler/MediaPipeUnityPlugin/pull/1311
                        yield return waitForEndOfFrame;
                        break;
                    case ImageReadMode.CPU: // CPU模式读取图像
                        yield return waitForEndOfFrame;
                        textureFrame.ReadTextureOnCPU(imageSource.GetCurrentTexture(), flipHorizontally, flipVertically); // 在CPU上读取纹理
                        image = textureFrame.BuildCPUImage(); // 构建CPU图像
                        textureFrame.Release(); // 释放纹理帧
                        break;
                    case ImageReadMode.CPUAsync: // CPU异步模式读取图像(默认)
                    default:
                        req = textureFrame.ReadTextureAsync(imageSource.GetCurrentTexture(), flipHorizontally, flipVertically); // 异步读取纹理
                        yield return waitUntilReqDone; // 等待读取完成

                        if (req.hasError) // 检查读取错误
                        {
                            Debug.LogWarning($"Failed to read texture from the image source");
                            continue;
                        }
                        image = textureFrame.BuildCPUImage(); // 构建CPU图像
                        textureFrame.Release(); // 释放纹理帧
                        break;
                }

                // 根据不同运行模式处理图像
                switch (taskApi.runningMode)
                {
                    case Tasks.Vision.Core.RunningMode.IMAGE: // 单图像模式
                        if (taskApi.TryDetect(image, imageProcessingOptions, ref result)) // 尝试检测手部特征点
                        {
                            _handLandmarkerResultAnnotationController.DrawNow(result); // 立即绘制结果

                            // 在主线程上直接处理结果
                            ProcessHandLandmarkerResult(result);
                        }
                        else
                        {
                            _handLandmarkerResultAnnotationController.DrawNow(default); // 没有检测到结果，绘制默认状态
                        }
                        break;
                    case Tasks.Vision.Core.RunningMode.VIDEO: // 视频模式
                        if (taskApi.TryDetectForVideo(image, GetCurrentTimestampMillisec(), imageProcessingOptions, ref result)) // 尝试为视频帧检测手部特征点
                        {
                            _handLandmarkerResultAnnotationController.DrawNow(result); // 立即绘制结果

                            // 在主线程上直接处理结果
                            ProcessHandLandmarkerResult(result);
                        }
                        else
                        {
                            _handLandmarkerResultAnnotationController.DrawNow(default); // 没有检测到结果，绘制默认状态
                        }
                        break;
                    case Tasks.Vision.Core.RunningMode.LIVE_STREAM: // 实时流模式
                        taskApi.DetectAsync(image, GetCurrentTimestampMillisec(), imageProcessingOptions); // 异步检测，结果通过回调函数返回
                        break;
                }
            }
        }

        /// <summary>
        /// 将Tasks组件的NormalizedLandmark转换为Mediapipe的NormalizedLandmark
        /// </summary>
        private Mediapipe.NormalizedLandmark ConvertToMediapipeLandmark(Tasks.Components.Containers.NormalizedLandmark landmark)
        {
            // 创建一个新的Mediapipe.NormalizedLandmark对象
            var proto = new Mediapipe.NormalizedLandmark();

            // 通过反射设置x, y, z属性
            var type = proto.GetType();
            type.GetProperty("X").SetValue(proto, landmark.x);
            type.GetProperty("Y").SetValue(proto, landmark.y);
            type.GetProperty("Z").SetValue(proto, landmark.z);

            return proto;
        }

        /// <summary>
        /// 检测是否握拳
        /// </summary>  
        private bool IsFist(NormalizedLandmarks landmarks)
        {
            // 检查食指、中指、无名指和小指是否弯曲
            // 通过比较指尖和指根的位置来判断
            bool isIndexFingerBent = landmarks.landmarks[8].y > landmarks.landmarks[6].y; // 食指
            bool isMiddleFingerBent = landmarks.landmarks[12].y > landmarks.landmarks[10].y; // 中指
            bool isRingFingerBent = landmarks.landmarks[16].y > landmarks.landmarks[14].y; // 无名指
            bool isPinkyBent = landmarks.landmarks[20].y > landmarks.landmarks[18].y; // 小指

            // 检查拇指是否也弯曲 (这是握拳与点赞的关键区别)
            bool isThumbBent = landmarks.landmarks[4].y > landmarks.landmarks[2].y;

            // 检查拇指是否在手掌内部 (握拳时拇指通常在手掌内)
            bool isThumbInside = landmarks.landmarks[4].x > landmarks.landmarks[9].x;

            // 如果所有手指都弯曲，且拇指不是向上状态，则认为是握拳
            return isIndexFingerBent && isMiddleFingerBent && isRingFingerBent && isPinkyBent &&
                   (isThumbBent || isThumbInside);
        }

        /// <summary>
        /// 检测是否为点赞手势（拇指向上，其他手指弯曲）
        /// </summary>
        private bool IsThumbsUp(NormalizedLandmarks landmarks)
        {
            // 检查拇指是否向上
            bool isThumbUp = landmarks.landmarks[4].y < landmarks.landmarks[3].y &&
                             landmarks.landmarks[3].y < landmarks.landmarks[2].y;

            // 检查其他四指是否弯曲
            bool isIndexFingerBent = landmarks.landmarks[8].y > landmarks.landmarks[6].y; // 食指
            bool isMiddleFingerBent = landmarks.landmarks[12].y > landmarks.landmarks[10].y; // 中指
            bool isRingFingerBent = landmarks.landmarks[16].y > landmarks.landmarks[14].y; // 无名指
            bool isPinkyBent = landmarks.landmarks[20].y > landmarks.landmarks[18].y; // 小指

            // 如果拇指向上且其他手指弯曲，则认为是点赞手势
            return isThumbUp && isIndexFingerBent && isMiddleFingerBent && isRingFingerBent && isPinkyBent;
        }

        /// <summary>
        /// 检测是否为数字"1"手势（食指伸直，其他手指弯曲）
        /// </summary>
        private bool IsNumberOne(NormalizedLandmarks landmarks)
        {
            // 检查食指是否伸直
            bool isIndexFingerStraight = landmarks.landmarks[8].y < landmarks.landmarks[6].y;

            // 检查其他手指是否弯曲
            bool isMiddleFingerBent = landmarks.landmarks[12].y > landmarks.landmarks[10].y;
            bool isRingFingerBent = landmarks.landmarks[16].y > landmarks.landmarks[14].y;
            bool isPinkyBent = landmarks.landmarks[20].y > landmarks.landmarks[18].y;
            bool isThumbBent = landmarks.landmarks[4].x < landmarks.landmarks[3].x;

            // 如果食指伸直且其他手指弯曲，则认为是"1"手势
            return isIndexFingerStraight && isMiddleFingerBent && isRingFingerBent && isPinkyBent && isThumbBent;
        }

        /// <summary>
        /// 检测是否为数字"2"手势（食指和中指伸直，其他手指弯曲）
        /// </summary>
        private bool IsNumberTwo(NormalizedLandmarks landmarks)
        {
            // 检查食指和中指是否伸直
            bool isIndexFingerStraight = landmarks.landmarks[8].y < landmarks.landmarks[6].y;
            bool isMiddleFingerStraight = landmarks.landmarks[12].y < landmarks.landmarks[10].y;

            // 检查其他手指是否弯曲
            bool isRingFingerBent = landmarks.landmarks[16].y > landmarks.landmarks[14].y;
            bool isPinkyBent = landmarks.landmarks[20].y > landmarks.landmarks[18].y;
            bool isThumbBent = landmarks.landmarks[4].x < landmarks.landmarks[3].x;

            // 如果食指和中指伸直且其他手指弯曲，则认为是"2"手势
            return isIndexFingerStraight && isMiddleFingerStraight && isRingFingerBent && isPinkyBent && isThumbBent;
        }

        /// <summary>
        /// 检测是否为数字"3"手势（食指、中指和无名指伸直，其他手指弯曲）
        /// </summary>
        private bool IsNumberThree(NormalizedLandmarks landmarks)
        {
            // 检查食指、中指和无名指是否伸直
            bool isIndexFingerStraight = landmarks.landmarks[8].y < landmarks.landmarks[6].y;
            bool isMiddleFingerStraight = landmarks.landmarks[12].y < landmarks.landmarks[10].y;
            bool isRingFingerStraight = landmarks.landmarks[16].y < landmarks.landmarks[14].y;

            // 检查其他手指是否弯曲
            bool isPinkyBent = landmarks.landmarks[20].y > landmarks.landmarks[18].y;
            bool isThumbBent = landmarks.landmarks[4].x < landmarks.landmarks[3].x;

            // 如果食指、中指和无名指伸直且其他手指弯曲，则认为是"3"手势
            return isIndexFingerStraight && isMiddleFingerStraight && isRingFingerStraight && isPinkyBent && isThumbBent;
        }

        /// <summary>
        /// 判断是否为左手
        /// </summary>
        private bool IsLeftHand(Classifications handedness)
        {
            // MediaPipe的手部分类通常包含"Left"和"Right"类别，
            // 但由于摄像头镜像效果，实际上可能是相反的
            // 检查第一个分类的标签是否为"Left"
            if (handedness.categories.Count > 0)
            {
                return handedness.categories[0].categoryName.Contains("Left");
            }
            return false;
        }

        /// <summary>
        /// 判断是否为右手
        /// </summary>
        private bool IsRightHand(Classifications handedness)
        {
            // 检查第一个分类的标签是否为"Right"
            if (handedness.categories.Count > 0)
            {
                return handedness.categories[0].categoryName.Contains("Right");
            }
            return false;
        }

        /// <summary>
        /// 将手部标准化坐标转换为世界坐标
        /// </summary>
        /// <param name="landmark">标准化的手部坐标</param>
        /// <returns>对应的世界坐标</returns>
        private Vector3 HandPositionToWorldPosition(Tasks.Components.Containers.NormalizedLandmark landmark)
        {
            if (mainCamera == null) return Vector3.zero;

            // 标准化坐标的(0,0)点在图像左上角，(1,1)在右下角
            // 需要将y坐标翻转，因为Unity的屏幕坐标系y轴朝上，而图像坐标系y轴朝下
            float screenX = landmark.x * UnityEngine.Screen.width;
            float screenY = (1 - landmark.y) * UnityEngine.Screen.height; // 翻转Y坐标

            // 将屏幕坐标转换为世界坐标
            Vector3 worldPos = mainCamera.ScreenToWorldPoint(new Vector3(screenX, screenY, effectZPosition));
            return worldPos;
        }

        /// <summary>
        /// 计算两只手间的中点位置（以手腕为基准点）
        /// </summary>
        private Vector3 CalculateHandsMidPoint(NormalizedLandmarks hand1, NormalizedLandmarks hand2)
        {
            if (mainCamera == null) return Vector3.zero;

            // 获取两只手的手腕位置（索引0是手腕）
            var wrist1 = hand1.landmarks[0];
            var wrist2 = hand2.landmarks[0];

            // 计算手腕在屏幕上的坐标
            float screen1X = wrist1.x * UnityEngine.Screen.width;
            float screen1Y = (1 - wrist1.y) * UnityEngine.Screen.height;

            float screen2X = wrist2.x * UnityEngine.Screen.width;
            float screen2Y = (1 - wrist2.y) * UnityEngine.Screen.height;

            // 计算中点的屏幕坐标
            float midScreenX = (screen1X + screen2X) / 2;
            float midScreenY = (screen1Y + screen2Y) / 2;

            // 转换为世界坐标
            Vector3 worldPos = mainCamera.ScreenToWorldPoint(new Vector3(midScreenX, midScreenY, effectZPosition));
            return worldPos;
        }

        /// <summary>
        /// 在主线程上处理手部检测结果和控制粒子特效
        /// </summary>
        private void ProcessHandLandmarkerResult(HandLandmarkerResult result)
        {
            // 如果没有检测到手或手势，则停止所有特效
            if (result.handLandmarks == null || result.handLandmarks.Count == 0 || result.handedness == null)
            {
                // 如果当前有任何特效在播放，则停止它们
                if (isPlayingClapEffect || isPlayingFistEffect || isPlayingThumbsUpEffect ||
                    isPlayingNumberOneEffect || isPlayingNumberTwoEffect || isPlayingNumberThreeEffect)
                {
                    StopAllParticleEffects();
                }
                return;
            }

            // 跟踪左右手的索引
            int leftHandIndex = -1;
            int rightHandIndex = -1;

            // 识别左右手
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

            // 检测右手点赞手势
            bool isRightHandThumbsUp = false;
            if (rightHandIndex >= 0 && rightHandIndex < result.handLandmarks.Count)
            {
                isRightHandThumbsUp = IsThumbsUp(result.handLandmarks[rightHandIndex]);
            }

            // 检测左手握拳手势
            bool isLeftHandFist = false;
            if (leftHandIndex >= 0 && leftHandIndex < result.handLandmarks.Count)
            {
                isLeftHandFist = IsFist(result.handLandmarks[leftHandIndex]);
            }

            // 检测左手数字手势
            bool isLeftHandNumberOne = false;
            bool isLeftHandNumberTwo = false;
            bool isLeftHandNumberThree = false;

            if (leftHandIndex >= 0 && leftHandIndex < result.handLandmarks.Count)
            {
                isLeftHandNumberOne = IsNumberOne(result.handLandmarks[leftHandIndex]);
                isLeftHandNumberTwo = IsNumberTwo(result.handLandmarks[leftHandIndex]);
                isLeftHandNumberThree = IsNumberThree(result.handLandmarks[leftHandIndex]);
            }

            // 如果右手做点赞手势，播放点赞特效
            if (isRightHandThumbsUp && !isPlayingThumbsUpEffect &&
                Time.time - lastThumbsUpEffectTime >= MIN_EFFECT_INTERVAL)
            {
                // 获取右手位置
                Vector3 handPosition = HandPositionToWorldPosition(result.handLandmarks[rightHandIndex].landmarks[0]);

                PlayThumbsUpParticleEffect(handPosition);
                lastThumbsUpEffectTime = Time.time;
                Debug.Log("检测到右手点赞手势，播放点赞粒子特效!");
                return; // 已处理点赞手势，跳过其他手势检测
            }
            else if (!isRightHandThumbsUp && isPlayingThumbsUpEffect)
            {
                // 如果不再检测到点赞手势但特效仍在播放，则停止特效
                StopThumbsUpParticleEffect();
            }

            // 如果左手握拳，播放握拳特效
            if (isLeftHandFist && !isPlayingFistEffect &&
                Time.time - lastFistEffectTime >= MIN_EFFECT_INTERVAL)
            {
                // 获取左手位置
                Vector3 handPosition = HandPositionToWorldPosition(result.handLandmarks[leftHandIndex].landmarks[0]);

                PlayFistParticleEffect(handPosition);
                lastFistEffectTime = Time.time;
                Debug.Log("检测到左手握拳，播放握拳粒子特效!");
                return; // 已处理握拳手势，跳过其他手势检测
            }
            else if (!isLeftHandFist && isPlayingFistEffect)
            {
                // 如果不再检测到握拳手势但特效仍在播放，则停止特效
                StopFistParticleEffect();
            }

            // 检测左手比"1"手势
            if (isLeftHandNumberOne && !isPlayingNumberOneEffect &&
                Time.time - lastNumberOneEffectTime >= MIN_EFFECT_INTERVAL)
            {
                Vector3 handPosition = HandPositionToWorldPosition(result.handLandmarks[leftHandIndex].landmarks[0]);
                PlayNumberOneParticleEffect(handPosition);
                lastNumberOneEffectTime = Time.time;
                Debug.Log("检测到左手比'1'手势，播放数字1粒子特效!");
                return; // 已处理手势，跳过其他手势检测
            }
            else if (!isLeftHandNumberOne && isPlayingNumberOneEffect)
            {
                StopNumberOneParticleEffect();
            }

            // 检测左手比"2"手势
            if (isLeftHandNumberTwo && !isPlayingNumberTwoEffect &&
                Time.time - lastNumberTwoEffectTime >= MIN_EFFECT_INTERVAL)
            {
                Vector3 handPosition = HandPositionToWorldPosition(result.handLandmarks[leftHandIndex].landmarks[0]);
                PlayNumberTwoParticleEffect(handPosition);
                lastNumberTwoEffectTime = Time.time;
                Debug.Log("检测到左手比'2'手势，播放数字2粒子特效!");
                return; // 已处理手势，跳过其他手势检测
            }
            else if (!isLeftHandNumberTwo && isPlayingNumberTwoEffect)
            {
                StopNumberTwoParticleEffect();
            }

            // 检测左手比"3"手势
            if (isLeftHandNumberThree && !isPlayingNumberThreeEffect &&
                Time.time - lastNumberThreeEffectTime >= MIN_EFFECT_INTERVAL)
            {
                Vector3 handPosition = HandPositionToWorldPosition(result.handLandmarks[leftHandIndex].landmarks[0]);
                PlayNumberThreeParticleEffect(handPosition);
                lastNumberThreeEffectTime = Time.time;
                Debug.Log("检测到左手比'3'手势，播放数字3粒子特效!");
                return; // 已处理手势，跳过其他手势检测
            }
            else if (!isLeftHandNumberThree && isPlayingNumberThreeEffect)
            {
                StopNumberThreeParticleEffect();
            }

            // 检查是否双手都被检测到，用于合掌检测
            if (leftHandIndex >= 0 && rightHandIndex >= 0)
            {
                // 计算两手之间的中点位置
                Vector3 midWorldPos = CalculateHandsMidPoint(result.handLandmarks[leftHandIndex], result.handLandmarks[rightHandIndex]);

                // 两只手的手腕位置，用于计算距离
                var leftHandWrist = result.handLandmarks[leftHandIndex].landmarks[0];
                var rightHandWrist = result.handLandmarks[rightHandIndex].landmarks[0];

                // 计算两个手腕之间的距离
                float distance = Vector3.Distance(
                    new Vector3(leftHandWrist.x, leftHandWrist.y, leftHandWrist.z),
                    new Vector3(rightHandWrist.x, rightHandWrist.y, rightHandWrist.z)
                );

                // 如果距离小于阈值，则认为双手合拢
                if (distance < handsCloseThreshold && !isPlayingClapEffect &&
                         Time.time - lastClapEffectTime >= MIN_EFFECT_INTERVAL)
                {
                    // 播放合掌粒子特效（在两手之间的中点位置）
                    PlayClapParticleEffect(midWorldPos);
                    lastClapEffectTime = Time.time;
                    Debug.Log("检测到双手合拢，播放合掌粒子特效! 距离: " + distance);
                }
                else if (distance >= handsCloseThreshold && isPlayingClapEffect)
                {
                    // 如果不再检测到合掌手势但特效仍在播放，则停止特效
                    StopClapParticleEffect();
                }
            }
            else if (isPlayingClapEffect)
            {
                // 如果合掌特效正在播放但现在没有双手，则停止特效
                StopClapParticleEffect();
            }
        }

        /// <summary>
        /// 停止所有粒子特效
        /// </summary>
        private void StopAllParticleEffects()
        {
            // 只有当各自的特效正在播放时才停止它们
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

            // 添加对新特效的处理
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
        /// 播放合掌粒子特效
        /// </summary>
        private void PlayClapParticleEffect(Vector3 position)
        {
            if (clapParticleEffect != null && !isPlayingClapEffect)
            {
                // 停止其他特效
                StopFistParticleEffect();
                StopThumbsUpParticleEffect();

                // 设置粒子系统位置
                clapParticleEffect.transform.position = position;

                // 先确保完全清除
                clapParticleEffect.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
                // 播放合掌特效
                clapParticleEffect.Play();
                isPlayingClapEffect = true;
            }
        }

        /// <summary>
        /// 停止合掌粒子特效
        /// </summary>
        private void StopClapParticleEffect()
        {
            if (clapParticleEffect != null && isPlayingClapEffect)
            {
                // 停止发射新粒子，但允许现有粒子完成生命周期
                clapParticleEffect.Stop(true, ParticleSystemStopBehavior.StopEmitting);
                // 状态标记会在Update中检查粒子系统是否还有活跃粒子
            }
        }

        /// <summary>
        /// 播放握拳粒子特效
        /// </summary>
        private void PlayFistParticleEffect(Vector3 position)
        {
            if (fistParticleEffect != null && !isPlayingFistEffect)
            {
                // 停止其他特效
                StopClapParticleEffect();
                StopThumbsUpParticleEffect();

                // 设置粒子系统位置
                fistParticleEffect.transform.position = position;

                // 先确保完全清除
                fistParticleEffect.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
                // 播放握拳特效
                fistParticleEffect.Play();
                isPlayingFistEffect = true;
            }
        }

        /// <summary>
        /// 停止握拳粒子特效
        /// </summary>
        private void StopFistParticleEffect()
        {
            if (fistParticleEffect != null && isPlayingFistEffect)
            {
                // 停止发射新粒子，但允许现有粒子完成生命周期
                fistParticleEffect.Stop(true, ParticleSystemStopBehavior.StopEmitting);
                // 状态标记会在Update中检查粒子系统是否还有活跃粒子
            }
        }

        /// <summary>
        /// 播放点赞粒子特效
        /// </summary>
        private void PlayThumbsUpParticleEffect(Vector3 position)
        {
            if (thumbsUpParticleEffect != null && !isPlayingThumbsUpEffect)
            {
                // 停止其他特效
                StopClapParticleEffect();
                StopFistParticleEffect();

                // 设置粒子系统位置
                thumbsUpParticleEffect.transform.position = position;

                // 先确保完全清除
                thumbsUpParticleEffect.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
                // 播放点赞特效
                thumbsUpParticleEffect.Play();
                isPlayingThumbsUpEffect = true;
            }
        }

        /// <summary>
        /// 停止点赞粒子特效
        /// </summary>
        private void StopThumbsUpParticleEffect()
        {
            if (thumbsUpParticleEffect != null && isPlayingThumbsUpEffect)
            {
                // 停止发射新粒子，但允许现有粒子完成生命周期
                thumbsUpParticleEffect.Stop(true, ParticleSystemStopBehavior.StopEmitting);
                // 状态标记会在Update中检查粒子系统是否还有活跃粒子
            }
        }

        /// <summary>
        /// 播放数字"1"粒子特效
        /// </summary>
        private void PlayNumberOneParticleEffect(Vector3 position)
        {
            if (numberOneParticleEffect != null && !isPlayingNumberOneEffect)
            {
                // 停止其他特效
                StopAllParticleEffects();

                // 设置粒子系统位置
                numberOneParticleEffect.transform.position = position;

                // 先确保完全清除
                numberOneParticleEffect.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
                // 播放特效
                numberOneParticleEffect.Play();
                isPlayingNumberOneEffect = true;
            }
        }

        /// <summary>
        /// 停止数字"1"粒子特效
        /// </summary>
        private void StopNumberOneParticleEffect()
        {
            if (numberOneParticleEffect != null && isPlayingNumberOneEffect)
            {
                numberOneParticleEffect.Stop(true, ParticleSystemStopBehavior.StopEmitting);
            }
        }

        /// <summary>
        /// 播放数字"2"粒子特效
        /// </summary>
        private void PlayNumberTwoParticleEffect(Vector3 position)
        {
            if (numberTwoParticleEffect != null && !isPlayingNumberTwoEffect)
            {
                // 停止其他特效
                StopAllParticleEffects();

                // 设置粒子系统位置
                numberTwoParticleEffect.transform.position = position;

                // 先确保完全清除
                numberTwoParticleEffect.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
                // 播放特效
                numberTwoParticleEffect.Play();
                isPlayingNumberTwoEffect = true;
            }
        }

        /// <summary>
        /// 停止数字"2"粒子特效
        /// </summary>
        private void StopNumberTwoParticleEffect()
        {
            if (numberTwoParticleEffect != null && isPlayingNumberTwoEffect)
            {
                numberTwoParticleEffect.Stop(true, ParticleSystemStopBehavior.StopEmitting);
            }
        }

        /// <summary>
        /// 播放数字"3"粒子特效
        /// </summary>
        private void PlayNumberThreeParticleEffect(Vector3 position)
        {
            if (numberThreeParticleEffect != null && !isPlayingNumberThreeEffect)
            {
                // 停止其他特效
                StopAllParticleEffects();

                // 先确保完全清除
                numberThreeParticleEffect.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
                // 播放特效
                numberThreeParticleEffect.Play();
                isPlayingNumberThreeEffect = true;
            }
        }

        /// <summary>
        /// 停止数字"3"粒子特效
        /// </summary>
        private void StopNumberThreeParticleEffect()
        {
            if (numberThreeParticleEffect != null && isPlayingNumberThreeEffect)
            {
                numberThreeParticleEffect.Stop(true, ParticleSystemStopBehavior.StopEmitting);
            }
        }

        /// <summary>
        /// 实时流模式下的检测结果回调函数
        /// </summary>
        /// <param name="result">手部特征点检测结果</param>
        /// <param name="image">输入图像</param>
        /// <param name="timestamp">时间戳</param>
        private void OnHandLandmarkDetectionOutput(HandLandmarkerResult result, Image image, long timestamp)
        {
            _handLandmarkerResultAnnotationController.DrawLater(result); // 稍后绘制结果（在下一帧渲染时）

            // 因为这个回调运行在后台线程，我们需要将结果传递到主线程
            // 将最新结果保存起来，然后在Update方法中处理
            lock (resultLock)
            {
                latestResult = result;
                hasNewResult = true;
            }
        }
    }
}