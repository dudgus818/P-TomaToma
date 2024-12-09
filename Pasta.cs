using System.Collections;
using UnityEngine;

public class Pasta : MonoBehaviour
{
    public Transform player;
    public float moveDistance = 3f;
    public float moveSpeed = 1f;
    public float approachSpeed = 2f;
    public float approachRange = 5f;
    private Vector3 startPosition;
    private bool movingLeft = true;
    private bool isApproaching = false;
    private SpriteRenderer spriteRenderer;
    private Animator animator;
    public Sprite hitSprite;
    private Sprite originalSprite;
    private Coroutine currentAnimationCoroutine;
    private int health = 6;
    private bool isHit = false;
    private bool isDie = false;

    void Start()
    {
        startPosition = transform.position;
        spriteRenderer = GetComponent<SpriteRenderer>();
        originalSprite = spriteRenderer.sprite;
        animator = GetComponent<Animator>();
        currentAnimationCoroutine = StartCoroutine(MovePattern());
    }

    void Update()
    {
        // 피격 상태 또는 죽은 상태라면 행동 중지
        if (isHit || isDie) return;

        float distanceToPlayer = Vector3.Distance(transform.position, player.position);

        // 플레이어 추격 로직
        if (distanceToPlayer <= approachRange && !isApproaching)
        {
            StopCurrentAction(); // 현재 동작 중지
            StartCoroutine(ApproachPlayer());
        }
        else if (!isApproaching && currentAnimationCoroutine == null)
        {
            currentAnimationCoroutine = StartCoroutine(MovePattern());
        }
    }

    private void TakeDamage(int damage)
    {
        health -= damage;

        if (health <= 0)
        {
            if (isHit)
            {
                StopCoroutine(HandleHit());
                spriteRenderer.sprite = originalSprite;
                isHit = false;
                animator.enabled = true;
            }

            if (!isDie)
                Die();
            return;
        }

        // 경직 상태가 이미 활성화된 경우에도 갱신
        if (!isHit)
        {
            isHit = true;
            animator.enabled = false;
            spriteRenderer.sprite = hitSprite;
        }

        // 코루틴 갱신
        StopAllCoroutines();
        StartCoroutine(HandleHit());
    }

    IEnumerator HandleHit()
    {
        yield return new WaitForSeconds(1f); // 1초 동안 멈춤

        // 피격 후 상태 복구
        spriteRenderer.sprite = originalSprite;
        isHit = false;
        animator.enabled = true;

        // 피격 후 이동 패턴 재개
        if (!isDie)
        {
            if (isApproaching)
            {
                StartCoroutine(ApproachPlayer());
            }
            else
            {
                currentAnimationCoroutine = StartCoroutine(MovePattern());
            }
        }
    }

    IEnumerator MovePattern()
    {
        while (!isApproaching && !isDie)
        {
            float targetX = movingLeft ? startPosition.x - 5f : startPosition.x + 5f;
            Vector3 targetPosition = new Vector3(targetX, transform.position.y, transform.position.z);

            float elapsedTime = 0f;
            while (elapsedTime < 3f)
            {
                if (isHit) yield break; // 피격 상태라면 중단

                transform.position = Vector3.MoveTowards(transform.position, targetPosition, moveSpeed * Time.deltaTime);
                elapsedTime += Time.deltaTime;
                yield return null;
            }

            movingLeft = !movingLeft;
            spriteRenderer.flipX = movingLeft;

            animator.SetBool("IsFly", false);
            animator.SetBool("IsIdle", true);
            yield return new WaitForSeconds(2f);

            animator.SetBool("IsFly", true);
            animator.SetBool("IsIdle", false);
        }
    }

    IEnumerator ApproachPlayer()
    {
        isApproaching = true;
        animator.SetBool("IsFly", true);
        animator.SetBool("IsIdle", false);

        while (Vector3.Distance(transform.position, player.position) > 0.1f)
        {
            if (isHit) yield break;

            // 플레이어를 벗어나면 추적 중지
            if (Vector3.Distance(transform.position, player.position) > approachRange)
            {
                isApproaching = false;
                currentAnimationCoroutine = StartCoroutine(MovePattern());
                yield break;
            }
            Vector3 direction = (player.position - transform.position).normalized;
            if (direction.x < 0)
            {
                spriteRenderer.flipX = true;
            }
            else
            {
                spriteRenderer.flipX = false; 
            }

            transform.position = Vector3.MoveTowards(transform.position, player.position, approachSpeed * Time.deltaTime);
            yield return null;
        }

        isApproaching = false;
        currentAnimationCoroutine = StartCoroutine(MovePattern());
    }
    private void Die()
    {
        if (isDie) return;

        isDie = true;
        animator.SetBool("IsDie", true);

        StopAllCoroutines();
        isApproaching = false;
        isHit = false;

        CircleCollider2D collider = GetComponent<CircleCollider2D>();
        if (collider != null)
        {
            collider.enabled = false;
        }
        AudioManager.Instance.PlaySFX(SFX.EnemyDie1);

        Destroy(gameObject, 0.8f);
    }
    private void StopCurrentAction()
    {
        if (currentAnimationCoroutine != null)
        {
            StopCoroutine(currentAnimationCoroutine);
            currentAnimationCoroutine = null;
        }

        isApproaching = false;
    }

    private void OnMouseDown()
    {
        if (Input.GetMouseButtonDown(0))
        {
            RaycastHit2D hit = Physics2D.Raycast(Camera.main.ScreenToWorldPoint(Input.mousePosition), Vector2.zero);
            if (hit.collider != null && hit.collider.gameObject == gameObject)
            {
                TakeDamage(1);
            }
        }
    }
}
