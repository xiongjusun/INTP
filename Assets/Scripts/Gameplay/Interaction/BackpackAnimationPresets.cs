using UnityEngine;

namespace INTP.Gameplay
{
    /// <summary>
    /// 背包UI预设动画曲线
    /// 可在Inspector中选择预设，或创建自定义曲线
    /// </summary>
    public static class BackpackAnimationPresets
    {
        /// <summary>
        /// 平滑缓入缓出 - 默认效果
        /// </summary>
        public static AnimationCurve SmoothEaseInOut => AnimationCurve.EaseInOut(0, 0, 1, 1);

        /// <summary>
        /// 弹性展开 - 展开时有回弹效果
        /// </summary>
        public static AnimationCurve ElasticExpand
        {
            get
            {
                var curve = new AnimationCurve();
                curve.AddKey(0f, 0f);
                curve.AddKey(0.3f, 1.2f);  // 过冲
                curve.AddKey(0.5f, 0.9f);  // 回弹
                curve.AddKey(0.7f, 1.05f); // 再过冲
                curve.AddKey(1f, 1f);      // 稳定
                return curve;
            }
        }

        /// <summary>
        /// 快速展开 - 快速展开，慢速收缩
        /// </summary>
        public static AnimationCurve FastExpand
        {
            get
            {
                var curve = new AnimationCurve();
                curve.AddKey(0f, 0f);
                curve.AddKey(0.6f, 1f);   // 快速完成
                curve.AddKey(1f, 1f);
                return curve;
            }
        }

        /// <summary>
        /// 慢速收缩 - 展开快，收缩慢
        /// </summary>
        public static AnimationCurve SlowCollapse
        {
            get
            {
                var curve = new AnimationCurve();
                curve.AddKey(0f, 0f);
                curve.AddKey(0.4f, 1f);
                curve.AddKey(1f, 1f);
                return curve;
            }
        }

        /// <summary>
        /// 泡泡膨胀效果 - 从小圆点弹性展开
        /// </summary>
        public static AnimationCurve BubblePop
        {
            get
            {
                var curve = new AnimationCurve();
                curve.AddKey(0f, 0f);
                curve.AddKey(0.2f, 0.05f);  // 开始膨胀
                curve.AddKey(0.5f, 1.15f);  // 过冲膨胀
                curve.AddKey(0.7f, 0.95f);   // 回弹
                curve.AddKey(0.85f, 1.02f);  // 微调
                curve.AddKey(1f, 1f);
                return curve;
            }
        }

        /// <summary>
        /// 线性 - 无缓动
        /// </summary>
        public static AnimationCurve Linear => AnimationCurve.Linear(0, 0, 1, 1);
    }
}
