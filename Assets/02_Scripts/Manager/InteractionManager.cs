using System;
using UnityEngine;

/// <summary>
/// 역할: PlayerGazeController와 PlayerInputProvider의 데이터를 바탕으로 
/// IInteractable 오브젝트(Object 레이어)의 상호작용(Reach)만을 전담하여 실행하는 매니저.
/// (이동 및 바닥 인디케이터 제어 로직은 PlayerUIController와 PlayerMovement로 완전 이관됨)
/// </summary>
public class InteractionManager : MonoBehaviour
{
    public static InteractionManager Instance { get; private set; }

    public event Action<bool> OnFocusChanged;

    private IInteractable _currentFocusedObject;
    private bool _isObjectReadyTriggered = false;

    private PlayerInputProvider _inputProvider;
    private PlayerGazeController _gazeController;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
        DontDestroyOnLoad(gameObject);
    }

    private void Start()
    {
        // 씬 내의 플레이어 오브젝트에서 신규 코어 모듈들을 찾아 연결합니다.
        // 플레이어 프리팹 최상단(Player_Root)에 부착되어 있어야 합니다.
        _inputProvider = FindObjectOfType<PlayerInputProvider>();
        _gazeController = FindObjectOfType<PlayerGazeController>();

        if (_inputProvider == null || _gazeController == null)
        {
            Debug.LogError("[InteractionManager] 플레이어 핵심 입력 모듈(PlayerInputProvider 또는 PlayerGazeController)을 찾을 수 없습니다.");
        }
    }

    private void Update()
    {
        if (_inputProvider == null || _gazeController == null) return;

        // 전역 Freeze 상태에서는 상호작용(Reach)을 차단합니다.
        if (FreezeManager.Instance != null && FreezeManager.Instance.IsFreezing)
        {
            ClearAllFocus();
            return;
        }

        Transform targetTransform = _gazeController.CurrentHoverTarget;
        bool isReady = _gazeController.IsTargetReady;

        if (targetTransform != null)
        {
            IInteractable interactable = targetTransform.GetComponent<IInteractable>();
            if (interactable != null)
            {
                if (_currentFocusedObject != interactable)
                {
                    ChangeObjectFocus(interactable);
                }
                else
                {
                    if (isReady)
                    {
                        if (!_isObjectReadyTriggered)
                        {
                            _isObjectReadyTriggered = true;
                            _currentFocusedObject.OnReadyStateReached();
                        }

                        // 신규 입력 시스템의 Reach(좌클릭) 판정
                        if (_inputProvider.IsGripTriggered)
                        {
                            _currentFocusedObject.OnInteract();
                            ClearObjectFocus();
                        }
                    }
                    else
                    {
                        _isObjectReadyTriggered = false;
                    }
                }
                return;
            }
        }

        // 유효한 IInteractable을 바라보지 않는다면 포커스 해제
        ClearAllFocus();
    }

    private void ChangeObjectFocus(IInteractable newInteractable)
    {
        ClearObjectFocus();
        _currentFocusedObject = newInteractable;
        _isObjectReadyTriggered = false;
        _currentFocusedObject.OnFocusEnter();
        OnFocusChanged?.Invoke(true);
    }

    private void ClearObjectFocus()
    {
        if (_currentFocusedObject != null)
        {
            _currentFocusedObject.OnFocusExit();
            _currentFocusedObject = null;
        }
        _isObjectReadyTriggered = false;
    }

    private void ClearAllFocus()
    {
        ClearObjectFocus();
        OnFocusChanged?.Invoke(false);
    }
}