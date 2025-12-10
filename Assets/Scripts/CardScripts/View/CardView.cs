// CardView3D.cs
using System;
using System.Collections;
using TMPro;
using UnityEngine;
#if TMP_PRESENT
using TMPro;
#endif

public enum AttackAnimationVariant
{
    Simple,     // Twoja domyœlna "naciskaj¹ca" animacja
    SpinLunge,  // "reverse / obrot" u¿ywane przy kill
    SlashSquash // trzecia animacja (u¿ywana gdy obie umieraj¹)
}

public class CardView : MonoBehaviour
{
    public CardInstance model;

    public TMP_Text nameTextTMP;
    public TMP_Text attackTextTMP;
    public TMP_Text healthTextTMP;
    public TMP_Text costTextTMP;
    public TMP_Text descriptionTextTMP;

    public void Setup(CardInstance instance)
    {
        model = instance;
        RefreshVisuals();
    }

    public void RefreshVisuals()
    {
        if (model == null) return;

        if (nameTextTMP != null) nameTextTMP.text = model.Name;
        if (descriptionTextTMP != null) descriptionTextTMP.text = model.Description;
        if (attackTextTMP != null) attackTextTMP.text = model.Attack.ToString();
        if (healthTextTMP != null) healthTextTMP.text = model.currentHealth.ToString();
        if (costTextTMP != null) costTextTMP.text = model.Cost.ToString();
    }

    public void SetCard(CardInstance data)
    {
        model = data;
        RefreshVisuals();
    }

    public void PlayDeathAnimation(Action onComplete)
    {
        StartCoroutine(DeathAnimationCoroutine(onComplete));
    }

    public void PlayAttackAnimationVariant(Transform target, AttackAnimationVariant variant, Action onComplete = null, float attackDuration = 0.18f, float attackOffset = 0.05f)
    {
        switch (variant)
        {
            case AttackAnimationVariant.Simple:
                StartCoroutine(AttackCoroutine(target, false, attackDuration, attackOffset, onComplete));
                break;

            case AttackAnimationVariant.SpinLunge:
                StartCoroutine(SpinLungeCoroutine(target, attackDuration * 1.1f, attackOffset * 1.2f, onComplete));
                break;

            case AttackAnimationVariant.SlashSquash:
                StartCoroutine(SlashSquashCoroutine(target, attackDuration * 1.0f, attackOffset * 0.8f, onComplete));
                break;
        }
    }

    private IEnumerator DeathAnimationCoroutine(Action onComplete)
    {
        float duration = 1f;
        float elapsed = 0f;
        Vector3 startScale = transform.localScale;
        Vector3 endScale = Vector3.zero;
        Vector3 startPos = transform.localPosition;
        Vector3 endPos = startPos + new Vector3(0, -0.2f, 0);
        CanvasGroup cg = GetComponent<CanvasGroup>();

        if (cg == null)
            cg = gameObject.AddComponent<CanvasGroup>();

        float startAlpha = cg.alpha;

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / duration;

            // delikatne ease-in-out
            t = t * t * (3f - 2f * t);

            transform.localScale = Vector3.Lerp(startScale, endScale, t);
            transform.localPosition = Vector3.Lerp(startPos, endPos, t);
            cg.alpha = Mathf.Lerp(startAlpha, 0f, t);

            yield return null;
        }

        onComplete?.Invoke();
    }

    private IEnumerator AttackCoroutine(Transform target, bool willKill, float attackDuration, float attackOffset, Action onComplete)
    {
        if (target == null)
        {
            onComplete?.Invoke();
            yield break;
        }

        Vector3 originalLocalPos = transform.localPosition;
        Quaternion originalLocalRot = transform.localRotation;

        // target world position
        Vector3 targetWorld = target.position;

        // --- prosty hit: lekko "naciskamy" do przodu (jak masz teraz) ---
        Vector3 dir = (targetWorld - transform.position).normalized;
        Vector3 attackPosWorld = transform.position + dir * attackOffset;
        Vector3 attackLocal = transform.parent != null ? transform.parent.InverseTransformPoint(attackPosWorld) : attackPosWorld;

        float half = attackDuration / 2f;
        float t = 0f;

        // forward
        while (t < half)
        {
            t += Time.deltaTime;
            float p = Mathf.SmoothStep(0f, 1f, t / half);
            transform.localPosition = Vector3.Lerp(originalLocalPos, attackLocal, p);
            transform.localRotation = Quaternion.Slerp(originalLocalRot, Quaternion.Euler(0f, 0f, Mathf.Lerp(0f, -10f, p)), p);
            yield return null;
        }

        yield return new WaitForSeconds(0.03f);

        // back
        t = 0f;
        while (t < half)
        {
            t += Time.deltaTime;
            float p = Mathf.SmoothStep(0f, 1f, t / half);
            transform.localPosition = Vector3.Lerp(attackLocal, originalLocalPos, p);
            transform.localRotation = Quaternion.Slerp(Quaternion.Euler(0f, 0f, -10f), originalLocalRot, p);
            yield return null;
        }

        transform.localPosition = originalLocalPos;
        transform.localRotation = originalLocalRot;

        onComplete?.Invoke();
        yield break;
    }

    private IEnumerator SlashSquashCoroutine(Transform target, float attackDuration, float attackOffset, Action onComplete)
    {
        if (target == null) { onComplete?.Invoke(); yield break; }
        Vector3 origPos = transform.localPosition;
        Quaternion origRot = transform.localRotation;
        Vector3 origScale = transform.localScale;

        Vector3 targetWorld = target.position;
        Vector3 dir = (targetWorld - transform.position).normalized;
        // Lekko boczny offset ¿eby nie waliæ idealnie po prostej (wygl¹da ¿ywiej)
        Vector3 side = Vector3.Cross(dir, Vector3.up).normalized * (0.05f);
        Vector3 attackWorld = transform.position + dir * attackOffset + side;
        Vector3 attackLocal = transform.parent != null ? transform.parent.InverseTransformPoint(attackWorld) : attackWorld;

        float half = attackDuration / 2f;
        float t = 0f;
        // forward (³uk - lekko w dó³ do góry)
        while (t < half)
        {
            t += Time.deltaTime;
            float p = Mathf.SmoothStep(0f, 1f, t / half);
            // delikatna krzywizna ³uku: dodaj offset w górê w po³owie lotu
            float arc = Mathf.Sin(p * Mathf.PI) * 0.07f; // wysokoœæ ³uku
            Vector3 lerp = Vector3.Lerp(origPos, attackLocal, p) + Vector3.up * arc;
            transform.localPosition = lerp;
            transform.localRotation = Quaternion.Slerp(origRot, Quaternion.Euler(0f, 0f, Mathf.Lerp(0f, -12f, p)), p);
            yield return null;
        }

        // Impact: squash (krótkie sp³aszczenie i rozci¹gniêcie)
        float squashDur = 0.06f;
        float s = 0f;
        while (s < squashDur)
        {
            s += Time.deltaTime;
            float sp = s / squashDur;
            // scale: szerzej w X/Z, mniej w Y
            transform.localScale = Vector3.Lerp(origScale, new Vector3(origScale.x * 1.06f, origScale.y * 0.92f, origScale.z * 1.06f), Mathf.SmoothStep(0f, 1f, sp));
            yield return null;
        }

        // restore scale quickly
        s = 0f;
        while (s < squashDur)
        {
            s += Time.deltaTime;
            float sp = s / squashDur;
            transform.localScale = Vector3.Lerp(new Vector3(origScale.x * 1.06f, origScale.y * 0.92f, origScale.z * 1.06f), origScale, Mathf.SmoothStep(0f, 1f, sp));
            yield return null;
        }

        yield return new WaitForSeconds(0.02f);

        // back
        t = 0f;
        while (t < half)
        {
            t += Time.deltaTime;
            float p = Mathf.SmoothStep(0f, 1f, t / half);
            transform.localPosition = Vector3.Lerp(attackLocal, origPos, p);
            transform.localRotation = Quaternion.Slerp(Quaternion.Euler(0f, 0f, -12f), origRot, p);
            yield return null;
        }

        transform.localPosition = origPos;
        transform.localRotation = origRot;
        transform.localScale = origScale;
        onComplete?.Invoke();
    }

    private IEnumerator SpinLungeCoroutine(Transform target, float attackDuration, float attackOffset, Action onComplete)
    {
        if (target == null) { onComplete?.Invoke(); yield break; }
        Vector3 origPos = transform.localPosition;
        Quaternion origRot = transform.localRotation;

        Vector3 targetWorld = target.position;
        Vector3 dir = (targetWorld - transform.position).normalized;
        Vector3 attackWorld = transform.position + dir * attackOffset;
        Vector3 attackLocal = transform.parent != null ? transform.parent.InverseTransformPoint(attackWorld) : attackWorld;

        float half = attackDuration / 2f;
        float t = 0f;

        // Forward + spin (rotate around local Y and small tilt)
        while (t < half)
        {
            t += Time.deltaTime;
            float p = Mathf.SmoothStep(0f, 1f, t / half);
            transform.localPosition = Vector3.Lerp(origPos, attackLocal, p);
            // spin: od 0 do 180 stopni (lub 90 dla subtelniejszego efektu)
            transform.localRotation = Quaternion.Slerp(origRot, origRot * Quaternion.Euler(0f, 180f * p, Mathf.Lerp(0f, -10f, p)), p);
            yield return null;
        }

        // ma³y „odbicie” (short knock)
        float bounceDur = 0.05f;
        float b = 0f;
        Vector3 bounceTarget = attackLocal + Vector3.back * 0.06f; // lekki cofniêcie
        while (b < bounceDur)
        {
            b += Time.deltaTime;
            float p = Mathf.SmoothStep(0f, 1f, b / bounceDur);
            transform.localPosition = Vector3.Lerp(attackLocal, bounceTarget, p);
            yield return null;
        }

        yield return new WaitForSeconds(0.03f);

        // back to origin (z rotacj¹ na 0)
        t = 0f;
        Quaternion currentRot = transform.localRotation;
        Vector3 currentPos = transform.localPosition;
        while (t < half)
        {
            t += Time.deltaTime;
            float p = Mathf.SmoothStep(0f, 1f, t / half);
            transform.localPosition = Vector3.Lerp(currentPos, origPos, p);
            transform.localRotation = Quaternion.Slerp(currentRot, origRot, p);
            yield return null;
        }

        transform.localPosition = origPos;
        transform.localRotation = origRot;
        onComplete?.Invoke();
    }

}
