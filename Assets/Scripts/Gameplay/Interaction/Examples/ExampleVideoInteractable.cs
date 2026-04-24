using UnityEngine;
using UnityEngine.Video;
using INTP.Foundation;

namespace INTP.Gameplay
{
    /// <summary>
    /// 示例视频交互物品
    /// 需要在场景中有一个 VideoPlayer 和 RenderTexture
    /// </summary>
    public class ExampleVideoInteractable : VideoInteractable
    {
        [Header("Video Settings")]
        [SerializeField] private VideoClip videoClip;
        [SerializeField] private VideoPlayer videoPlayer;
        [SerializeField] private RenderTexture renderTexture;
        [SerializeField] private GameObject videoScreen; // 视频显示的屏幕
        [SerializeField] private Material screenMaterial; // 应用于屏幕的材质

        [Header("Settings")]
        [SerializeField] private float fadeInDuration = 0.5f;
        [SerializeField] private bool pauseGameWhilePlaying = true;

        private bool _isPlaying = false;

        protected override void OnVideoPlay()
        {
            if (_isPlaying) return;
            _isPlaying = true;

            // 设置视频材质
            if (screenMaterial != null && renderTexture != null)
            {
                screenMaterial.mainTexture = renderTexture;
            }

            // 播放视频
            if (videoPlayer != null && videoClip != null)
            {
                videoPlayer.clip = videoClip;
                videoPlayer.targetTexture = renderTexture;
                videoPlayer.Play();

                if (videoScreen != null)
                {
                    videoScreen.SetActive(true);
                }

                // 视频结束时回调
                videoPlayer.loopPointReached += OnVideoEnded;
            }

            Debug.Log($"Playing video: {videoTitle}");
        }

        private void OnVideoEnded(VideoPlayer vp)
        {
            _isPlaying = false;

            if (videoScreen != null)
            {
                videoScreen.SetActive(false);
            }

            Debug.Log($"Video ended: {videoTitle}");
        }

        /// <summary>
        /// 停止视频播放（外部调用）
        /// </summary>
        public void StopVideo()
        {
            if (videoPlayer != null)
            {
                videoPlayer.Stop();
                videoPlayer.loopPointReached -= OnVideoEnded;
            }

            if (videoScreen != null)
            {
                videoScreen.SetActive(false);
            }

            _isPlaying = false;
        }
    }
}
