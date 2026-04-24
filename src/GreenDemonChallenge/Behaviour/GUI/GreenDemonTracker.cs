using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using GreenDemonChallenge.Data;
using pworld.Scripts.Extensions;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace GreenDemonChallenge.Behaviour.GUI;

public class GreenDemonTracker : UIBehaviour
{
    private static readonly Color RedTint = new(0.8862745f, 0.20784314f, 0.14117648f);
    private GreenDemon? m_demon;

    public CanvasGroup m_group = null!;
    private RectTransform m_rectTransform = null!;

    private RectTransform m_rotatorTransform = null!;
    private RectTransform m_demonTransform = null!;

    private RawImage m_demonImage = null!;

    public static List<GreenDemonTracker> AllTrackers = [];
    
    private Camera m_camera = null!;
    private bool m_isValid = true;
    private bool m_isVisible = true;
    private bool m_isFading = true;

    private bool m_visibleOnScreen = false;
    private bool m_noVisuals = false;

    private float m_alphaTarget;
    private float m_distanceFromCam;
    private float m_demonScale;
    private Vector3 m_rawPosition;
    private bool m_isTinting;

    private float m_tintingTarget;
    private float m_maxRadius = 150f;
    private float m_minRadius = .5f;
    private float m_currentTinting;
    private bool m_isTinted;

    public AnimationCurve m_pulseTintCurve = AnimationCurve.EaseInOut(0, 0, 1, 1);
    
    public GreenDemon Demon
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get => m_demon ?? throw new InvalidOperationException();
        set
        {
            if (m_demon)
            {
                m_demon.OnPlayerCaught -= FadeOutAndDie;
                m_demon.OnShrink -= FadeOutAndDie;
            }

            m_demon = value;

            if (m_demon)
            {
                m_demon.OnPlayerCaught += FadeOutAndDie;
                m_demon.OnShrink += FadeOutAndDie;

                m_maxRadius = m_demon.source.maxDistance;
                m_minRadius = m_demon.m_catchRadius;
            } 
        }
    }

    protected override void Start()
    {
        base.Start();

        AllTrackers.Add(this);
        
        RefreshTrackerVisibility();
        
        if (m_alphaTarget != m_group.alpha)
        {
            m_isFading = true;
        }
    }
    
    public Vector2 CurrentPosition
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get => m_rectTransform.anchoredPosition;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        set => m_rectTransform.anchoredPosition = value;
    }

    private Vector3 GetClosestOutsidePosition(Vector3 trackPos)
    {
        var sizes = (m_rectTransform.rect.size / 2f);
        var trackerRect = GreenDemonGUIManager.Instance.trackerScreenTransform.rect;

        var closestPos = new Vector3(trackPos.x, trackPos.y, trackPos.z)
        {
            z = 0
        };

        var canvasCenter = trackerRect.center.xyo();

        closestPos -= canvasCenter;

        var dx = (trackerRect.width / 2f - sizes.x) / Mathf.Abs(closestPos.x);
        var dy = (trackerRect.height / 2f - sizes.y) / Mathf.Abs(closestPos.y);

        if (dx < dy)
        {
            var angle = Vector3.SignedAngle(Vector3.right, closestPos, Vector3.forward);
            closestPos.x = Mathf.Sign(closestPos.x) * ((trackerRect.width / 2f) - sizes.x);
            closestPos.y = Mathf.Tan(Mathf.Deg2Rad * angle) * closestPos.x;
        }
        else
        {
            var angle = Vector3.SignedAngle(Vector3.up, closestPos, Vector3.forward);
            closestPos.y = Mathf.Sign(closestPos.y) * ((trackerRect.height / 2f) - sizes.y);
            closestPos.x = -Mathf.Tan(Mathf.Deg2Rad * angle) * closestPos.y;
        }

        closestPos += canvasCenter;

        return closestPos;
    }

    private void OnGUI()
    {
        if (m_demon && m_isValid)
        {

            if (m_noVisuals || (!m_visibleOnScreen && m_camera.IsVisibleToCamera(m_demon.Center)))
            {
                if (m_isVisible)
                {
                    FadeOut();
                }
            }
            else
            {
                if (!m_isVisible && m_distanceFromCam <= (m_maxRadius * m_maxRadius))
                {
                    FadeIn();
                }
            }

            if (ShouldUpdatePosition)
            {
                var currentPos = m_rawPosition;
                var outside = false;

                if (m_rawPosition.z >= 0f)
                {
                    currentPos.z = 0f;
                }
                else
                {
                    currentPos *= -1;
                    outside = true;
                }

                if (outside || !GreenDemonGUIManager.Instance.trackerScreenTransform.rect.Contains(currentPos.xy()))
                {
                    currentPos = GetClosestOutsidePosition(currentPos);
                    m_rotatorTransform.rotation = Quaternion.Euler(GetTrackerRotation(currentPos));
                }
                else
                {
                    m_rotatorTransform.rotation = Quaternion.Euler(Vector3.zero);
                    // Make the arrow point directly at the demon.
                    currentPos.y += 0;
                }
                

                m_demonTransform.localScale = m_demonScale.ToVec();

                var scale = m_demonScale.ToVec();
                scale.z = 1f;
                m_demonTransform.localScale = scale;

                CurrentPosition = currentPos.xy();
            }
        }
    }

    private Vector3 GetTrackerRotation(Vector3 trackPos)
    {
        var trackerRect = GreenDemonGUIManager.Instance.trackerScreenTransform.rect;

        var canvasCenter = trackerRect.center;
        var angle = Vector3.SignedAngle(Vector3.down, trackPos - canvasCenter.xyo(), Vector3.forward);

        return new Vector3(0f, 0f, angle);
    }

    protected override void Awake()
    {
        m_camera ??= MainCamera.instance.cam ?? throw new InvalidOperationException();
        m_rectTransform ??= GetComponent<RectTransform>();
        m_group ??= GetComponent<CanvasGroup>();

        m_rotatorTransform ??= transform.Find("DemonArrowRotator").GetComponent<RectTransform>();
        m_demonTransform ??= transform.Find("DemonImage").GetComponent<RectTransform>();
        m_demonImage ??= m_demonTransform.GetComponent<RawImage>();
    }

    private void FixedUpdate()
    {
        if (m_demon)
        {
            m_rawPosition = m_camera.WorldToScreenPoint(m_demon.Center) -
                            new Vector3(Screen.width, Screen.height, 0) * 0.5f;

            m_distanceFromCam = Vector3.SqrMagnitude(m_camera.transform.position - m_demon.Center);

            if (m_distanceFromCam > (m_maxRadius * m_maxRadius))
            {
                m_demonScale = 0.025f;

                if (m_isVisible)
                {
                    FadeOut();
                }
            }
            else
            {
                m_demonScale = Util.RangeLerp(0.025f,
                    1f,
                    (m_maxRadius * m_maxRadius),
                    (m_minRadius * m_minRadius),
                    m_distanceFromCam
                );

                if (m_demon.IsChasingLocalPlayer)
                {
                    if (!m_isTinted)
                    {
                        m_tintingTarget = 1f;
                        m_isTinting = true;
                    }
                }
                else
                {
                    if (m_isTinted)
                    {
                        m_tintingTarget = 0f;
                        m_isTinting = true;
                    }
                }
            }

        }

        UpdateTrackerTransitives();
    }

    private void UpdateTrackerTransitives()
    {
        if (m_isTinting)
        {
            m_currentTinting = FRILerp.Lerp(m_currentTinting, m_tintingTarget, 40f);

            m_isTinting = !Mathf.Approximately(m_tintingTarget, m_currentTinting);

            if (!m_isTinting)
            {
                m_isTinted = Mathf.Approximately(m_tintingTarget, 1f);
            }
        }

        if (m_isFading)
        {
            m_group.alpha = FRILerp.Lerp(m_group.alpha, m_alphaTarget, 40f);
            m_isFading = !Mathf.Approximately(m_alphaTarget, m_group.alpha);

            if (!m_isFading)
            {
                m_isVisible = Mathf.Approximately(m_alphaTarget, 1f);

                if (!m_isVisible && !m_isValid)
                {
                    Destroy(gameObject);
                }
            }
        }

        m_demonImage.color = Color.Lerp(Color.white,
            Color.Lerp(Color.white, RedTint,
                m_pulseTintCurve.Evaluate(Time.time)), m_currentTinting);
    }
    
    

    private bool ShouldUpdatePosition
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get => m_distanceFromCam <= (m_maxRadius * m_maxRadius) ||
               (m_isVisible || (!m_isVisible && IsFadingIn));
    }

    private bool IsFadingOut
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get => m_isFading && Mathf.Approximately(m_alphaTarget, 0f);
    }

    private bool IsFadingIn
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get => m_isFading && Mathf.Approximately(m_alphaTarget, 1f);
    }

    protected override void OnDestroy()
    {
        AllTrackers.Remove(this);
        base.OnDestroy();
    }

    private void FadeOut()
    {
        if (m_isValid)
        {
            m_alphaTarget = 0f;
            m_isFading = true;
        }
    }

    private void FadeIn()
    {
        if (m_isValid)
        {
            m_alphaTarget = 1f;
            m_isFading = true;
        }
    }

    private void FadeOutAndDie()
    {
        if (m_isValid)
        {
            m_isValid = false;
            m_alphaTarget = 0f;
            m_isFading = true;
        }
    }

    public void RefreshTrackerVisibility()
    {
        switch (GreenDemonChallenge.GreenDemonTrackerSetting.Value)
        {
            case GreenDemonTrackerSettings.OFFSCREEN:
                m_visibleOnScreen = false;
                m_noVisuals = false;
                break;
            case GreenDemonTrackerSettings.ALWAYS:
                m_visibleOnScreen = true;
                m_noVisuals = false;
                break;
            case GreenDemonTrackerSettings.NEVER:
                m_noVisuals = false;
                break;
            default:
                throw new ArgumentOutOfRangeException(nameof(GreenDemonChallenge.GreenDemonTrackerSetting));
        }
    }
}