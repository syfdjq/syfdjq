// Copyright (c) 2023 homuler
//
// Use of this source code is governed by an MIT-style
// license that can be found in the LICENSE file or at
// https://opensource.org/licenses/MIT.

using Mediapipe.Tasks.Vision.HandLandmarker;

namespace Mediapipe.Unity.Sample.HandLandmarkDetection
{
    /// <summary>
    /// �ֲ���������������
    /// ����������������HandLandmarker����Ĳ���������
    /// </summary>
    public class HandLandmarkDetectionConfig
    {
        /// <summary>
        /// Ӳ������ί��ѡ��
        /// ����ģ������������Ӳ����(CPU/GPU)
        /// ��Windows��macOS��Ĭ��ʹ��CPU��������ƽ̨(���ƶ��豸)��Ĭ��ʹ��GPU
        /// </summary>
        public Tasks.Core.BaseOptions.Delegate Delegate { get; set; } =
#if UNITY_EDITOR_WIN || UNITY_STANDALONE_WIN || UNITY_EDITOR_OSX || UNITY_STANDALONE_OSX
          Tasks.Core.BaseOptions.Delegate.CPU;
#else
        Tasks.Core.BaseOptions.Delegate.GPU;
#endif

        /// <summary>
        /// ͼ���ȡģʽ
        /// ȷ����δ�Unity�����ж�ȡͼ������
        /// CPUAsync: �첽��GPU��ȡ��CPU (Ĭ�ϣ����ܽϺ�)
        /// CPU: ͬ����GPU��ȡ��CPU
        /// GPU: ֱ����GPU�ϴ���
        /// </summary>
        public ImageReadMode ImageReadMode { get; set; } = ImageReadMode.CPUAsync;

        /// <summary>
        /// ����ģʽ
        /// IMAGE: ����ͼ����ģʽ
        /// VIDEO: ��Ƶ����ģʽ(����֮֡���������)
        /// LIVE_STREAM: ʵʱ������ģʽ(�첽����ʹ�ûص�)
        /// Ĭ��ʹ��ʵʱ��ģʽ���ʺ�����ͷ����
        /// </summary>
        public Tasks.Vision.Core.RunningMode RunningMode { get; set; } = Tasks.Vision.Core.RunningMode.LIVE_STREAM;

        /// <summary>
        /// �����������
        /// ����ģ�Ϳ���ͬʱ�����ֵ�������Ĭ��Ϊ1
        /// ���Ӵ�ֵ���Լ���ֻ�֣��������Ӽ�����
        /// </summary>
        public int NumHands { get; set; } = 2;

        /// <summary>
        /// �ֲ������С���Ŷ�
        /// �ֲ����������С���Ŷ���ֵ�����ڴ�ֵ�ļ�����������˵�
        /// ȡֵ��Χ0-1��Ĭ��0.5
        /// </summary>
        public float MinHandDetectionConfidence { get; set; } = 0.5f;

        /// <summary>
        /// �ֲ�������С���Ŷ�
        /// �ֲ������������ж����ֲ����ڵ���С���Ŷ���ֵ
        /// ȡֵ��Χ0-1��Ĭ��0.5
        /// </summary>
        public float MinHandPresenceConfidence { get; set; } = 0.5f;

        /// <summary>
        /// �ֲ�������С���Ŷ�
        /// ����Ƶ/ʵʱ��ģʽ�£����ھ����Ƿ����������ǰ��⵽����
        /// ȡֵ��Χ0-1��Ĭ��0.5
        /// </summary>
        public float MinTrackingConfidence { get; set; } = 0.5f;

        /// <summary>
        /// ģ���ļ�·��
        /// ָ���ֲ���������ģ���ļ���·��
        /// </summary>
        public string ModelPath => "hand_landmarker.bytes";

        /// <summary>
        /// ����������HandLandmarkerѡ��
        /// ���ݵ�ǰ�����������ڳ�ʼ��HandLandmarker��ѡ�����
        /// </summary>
        /// <param name="resultCallback">����ص�����������LIVE_STREAMģʽ��ʹ��</param>
        /// <returns>���úõ�HandLandmarkerOptions����</returns>
        public HandLandmarkerOptions GetHandLandmarkerOptions(HandLandmarkerOptions.ResultCallback resultCallback = null)
        {
            return new HandLandmarkerOptions(
              new Tasks.Core.BaseOptions(Delegate, modelAssetPath: ModelPath), // ����ѡ�����Ӳ���������ú�ģ��·��
              runningMode: RunningMode, // ����ģʽ
              numHands: NumHands, // �����������
              minHandDetectionConfidence: MinHandDetectionConfidence, // �ֲ������С���Ŷ�
              minHandPresenceConfidence: MinHandPresenceConfidence, // �ֲ�������С���Ŷ�
              minTrackingConfidence: MinTrackingConfidence, // �ֲ�������С���Ŷ�
              resultCallback: resultCallback // ����ص�����(ʵʱ��ģʽ����Ҫ)
            );
        }
    }
}
