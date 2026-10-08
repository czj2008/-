using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using static UnityEngine.UI.Image;
public class ActorView : MonoBehaviour
{
    #region 引用
    [Header("引用（从 Hierarchy 拖进来）")]
    [Tooltip("位移动画的作用目标：Visual 空物体")]
    public Transform Visual;

    [Tooltip("角色的 SpriteRenderer（单张立绘本体）")]
    public SpriteRenderer Body;

    [Header("朝向")]
    [Tooltip("+1 = 面朝右（玩家），-1 = 面朝左（敌人）")]
    public int Facing = 1;

    [Tooltip("勾上：素材本身朝左，Facing 变化时自动翻转贴图")]
    public bool AutoFlipSprite = true;

    [Header("手感参数（想改打击感就调这里）")]
    [Tooltip("受击后退距离（世界单位）")]
    public float HitKnockback = 0.34f;

    [Tooltip("攻击前冲距离（世界单位）")]
    public float AttackDistance = 1.2f;

    [Tooltip("闪白持续时间")]
    public float FlashDuration = 0.07f;

    [Tooltip("受击时闪的颜色：玩家用淡青，敌人用淡橙，各自更明显")]
    public Color FlashColor = new Color(1f, 0.65f, 0.65f, 1f);

    [Tooltip("头顶血条")]
    public HealthBar HpBar;
    #endregion

    private Vector3 _visualHome;
    private Vector3 _bodyHomeScale; 
    private Coroutine _hitRoutine;
    private Coroutine _lungeRoutine;
    private Coroutine _idleRoutine;
    private Color? _bodyColor;

    private void Awake()
    {
        Initialize();
    }

    private void Start()
    {
        IdleBreathe();
    }
    public void Initialize()
    {
        if (Visual == null) Visual = transform;
        if (Body == null) Body = GetComponentInChildren<SpriteRenderer>();

        _visualHome = Visual.localPosition;
        _bodyHomeScale = Body != null ? Body.transform.localScale : Vector3.one;
        _bodyColor = Body != null ? (Color?)Body.color : null;
    }

    #region 角色呼吸
    public void IdleBreathe()
    {
        if (_idleRoutine != null) StopCoroutine(_idleRoutine);
        _idleRoutine = StartCoroutine(IdleRoutine());
    }

    private IEnumerator IdleRoutine()
    {
        float phase = Random.Range(0f, Mathf.PI * 2f);

        while (true)
        {

            if (_lungeRoutine == null && _hitRoutine == null)
            {
                float wave = Mathf.Sin(Time.time * Mathf.PI + phase) * 0.5f + 0.5f;   // 0~1，周期 2 秒

                Visual.localPosition = _visualHome + new Vector3(0f, wave * 0.035f, 0f);
                Body.transform.localScale = new Vector3(
                    _bodyHomeScale.x * (1f - wave * 0.008f),
                    _bodyHomeScale.y * (1f + wave * 0.012f),
                    _bodyHomeScale.z);
            }
            yield return null;
        }
    }
    #endregion

    #region 受击表现

    private IEnumerator HitRoutine()
    {
        try
        {
            Vector3 origin = _visualHome;
            Vector3 back = origin + new Vector3(-HitKnockback * Mathf.Sign(Facing), 0.06f, 0f);

            yield return Tween.MoveLocal(Visual, origin, back, 0.05f);   // 一顿
            yield return Tween.MoveLocal(Visual, back, origin, 0.11f);   // 一弹
        }
        finally { _hitRoutine = null; }
    }
    #endregion

    #region 出招位移 LungeRoutine
    public IEnumerator Lunge(float distance = -1f, float holdSeconds = 0.07f)
    {
        if (Visual == null) yield break;
        if (distance < 0f) distance = AttackDistance;

        if (_lungeRoutine != null) StopCoroutine(_lungeRoutine);
        _lungeRoutine = StartCoroutine(LungeRoutine(distance, holdSeconds));
        yield return _lungeRoutine;
    }
   

    private IEnumerator LungeRoutine(float distance, float holdSeconds)
    {
        try
        {
            float dir = Mathf.Sign(Facing);

            // 冲过去的同时把立绘横向拉长一点（挤压拉伸，比单纯平移更有冲击力）
            Vector3 target = _visualHome + new Vector3(distance * dir, 0.08f, 0f);
            Vector3 stretched = new Vector3(_bodyHomeScale.x * 0.88f, _bodyHomeScale.y * 1.06f, 1f);
            Quaternion lean = Quaternion.Euler(0f, 0f, -8f * dir);   // 前倾 8 度

            // 1) 冲出（快）+ 前倾 + 拉长
            StartCoroutine(Tween.RotateLocal(Body.transform, Quaternion.identity, lean, 0.13f));
            StartCoroutine(Tween.ScaleLocal(Body.transform, _bodyHomeScale, stretched, 0.13f));
            yield return Tween.MoveLocal(Visual, _visualHome, target, 0.13f);

            // 2) 到位停顿 ← 就是这里，调用方在这一刻结算伤害
            yield return Tween.WaitRealtime(holdSeconds);

            // 3) 回位（稍慢，形成"快出慢收"的节奏）
            StartCoroutine(Tween.RotateLocal(Body.transform, lean, Quaternion.identity, 0.18f));
            StartCoroutine(Tween.ScaleLocal(Body.transform, stretched, _bodyHomeScale, 0.18f));
            yield return Tween.MoveLocal(Visual, target, _visualHome, 0.18f);
        }
        finally
        {
            _lungeRoutine = null;
        }
    }
    #endregion

    #region 受击闪
    public IEnumerator HitReaction(Color flashColor, float flashDuration = -1f)
    {
        if (flashDuration < 0f) flashDuration = FlashDuration;

        if (_hitRoutine != null) StopCoroutine(_hitRoutine);
        _hitRoutine = StartCoroutine(HitRoutine());
        yield return FlashRoutine(flashColor, flashDuration);
    }

    private IEnumerator FlashRoutine(Color flashColor, float duration)
    {
        if (Body == null || _bodyColor == null) yield break;

        yield return Tween.ColorTo(Body, _bodyColor.Value, flashColor, duration * 0.25f);
        yield return Tween.ColorTo(Body, flashColor, _bodyColor.Value, duration * 0.75f);
    }
    #endregion

    #region 死亡
    public IEnumerator Die()
    {
        if (Body == null) yield break;

        Color faded = Body.color * 0.45f;
        faded.a = 0.6f;

        StartCoroutine(Tween.ScaleLocal(Body.transform, Body.transform.localScale, Body.transform.localScale * 0.7f, 0.35f));
        yield return Tween.ColorTo(Body, Body.color, faded, 0.35f);
    }
    #endregion

    #region 朝向
    public void SetFacing(int facing)
    {
        Facing = facing >= 0 ? 1 : -1;
        if (AutoFlipSprite && Body != null)
        {
            // 素材默认朝右：面朝左时才翻转
            Body.flipX = Facing < 0;
        }
    }
    #endregion

    [ContextMenu("测试：播放受击")]
    public void TestHit()
    {
        StartCoroutine(HitReaction(FlashColor));
    }

    [ContextMenu("测试：播放攻击")]
    public void TestLunge()
    {
        StartCoroutine(Lunge());
    }
}
