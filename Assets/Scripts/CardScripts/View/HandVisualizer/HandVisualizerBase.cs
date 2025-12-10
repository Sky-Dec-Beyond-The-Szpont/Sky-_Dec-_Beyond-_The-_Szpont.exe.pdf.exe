using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public abstract class HandVisualizerBase : MonoBehaviour
{
    [Header("Owner & Prefab")]
    public PlayerModel owner;        // przypisujemy w runtime z GameLogicManager
    public GameObject cardPrefab;
    public Transform handAnchor;     // anchor dla uk³adu rêki
    public SoundManager soundManager;

    [Header("Slots (po stronie w³aœciciela)")]
    public Transform[] slotTransforms; // sloty mapping: index -> transform na stole

    [Header("Layout")]
    public float cardSpacing = 1.5f;
    public float curveHeight = 0.4f;

    [Header("Animation")]
    public float moveDuration = 0.35f;

    [Header("Deck & Draw Animation")]
    public Transform deckPileTransform;
    public float drawMoveDuration = 0.5f;

    protected List<GameObject> spawnedCards = new List<GameObject>();

    private void Start()
    {
        if (soundManager == null)
            soundManager = FindFirstObjectByType<SoundManager>();
    }

    // publiczna metoda do rysowania rêki (wywo³uj po DrawToHand / po inicjacji)
    [ContextMenu("Refresh Hand")]
    public virtual void RefreshHand()
    {
        if (owner == null || owner.hand == null)
        {
            foreach (var go in spawnedCards) if (go != null) Destroy(go);
            spawnedCards.Clear();
            return;
        }

        // Start coroutine to handle animated refresh
        Debug.Log("StopAllCoroutines called here\n" + System.Environment.StackTrace);
        StopAllCoroutines(); // bezpieczne: zatrzymujemy poprzednie animacje rêki

        if ((spawnedCards == null || spawnedCards.Count == 0) && owner.hand.Count > 0)
        {
            StartCoroutine(AnimateInitialHandDraw());
            return;
        }

        StartCoroutine(RefreshHandRoutine());
    }

    protected IEnumerator RefreshHandRoutine()
    {
        // Guard
        if (owner == null || owner.hand == null)
        {
            // if no owner, just clear visuals
            foreach (var go in spawnedCards) if (go != null) Destroy(go);
            spawnedCards.Clear();
            yield break;
        }

        var existingMap = new Dictionary<CardInstance, GameObject>();

        foreach (var go in spawnedCards)
        {
            if (go == null)
                continue;

            var view = go.GetComponent<CardView>();
            if (view == null || view.model == null)
                continue;

            if (!existingMap.ContainsKey(view.model))
                existingMap[view.model] = go;
        }

        int newCount = owner.hand.Count;
        float totalWidth = Mathf.Max(0, (newCount - 1) * cardSpacing);

        Vector3[] targetPositions = new Vector3[newCount];
        Quaternion[] targetRotations = new Quaternion[newCount];

        for (int i = 0; i < newCount; i++)
        {
            Vector3 localTarget = handAnchor.right * (i * cardSpacing - totalWidth / 2f)
                + handAnchor.up * Mathf.Sin((i - (newCount - 1) / 2f) * 0.3f) * curveHeight;
            targetPositions[i] = handAnchor.position + localTarget;
            targetRotations[i] = handAnchor.rotation * Quaternion.Euler(0f, (i - (newCount - 1) / 2f) * 8f, 0f);
        }

        // 3) Move existing visuals to their new positions (animate)
        // We'll start coroutines for those found in map, and keep track of which indices are filled
        bool[] filled = new bool[newCount];
        var moveCoroutines = new List<Coroutine>();
        var newSpawned = new List<GameObject>(newCount);

        for (int i = 0; i < newCount; i++)
        {
            var inst = owner.hand[i];
            if (inst != null && existingMap.TryGetValue(inst, out GameObject go))
            {
                // animate this existing visual to targetPositions[i]
                // ensure it's parented under handAnchor so local positioning becomes consistent later
                go.transform.SetParent(handAnchor, true);

                // start animation
                if (this is PlayerHandVisualizer)
                    moveCoroutines.Add(StartCoroutine(MoveTransformTo(go.transform, targetPositions[i], targetRotations[i], moveDuration)));
                else
                    moveCoroutines.Add(StartCoroutine(MoveTransformTo(go.transform, targetPositions[targetPositions.Length - i - 1], targetRotations[targetPositions.Length - i - 1], moveDuration)));

                // mark as used and remove from existingMap so duplicates won't be reused
                existingMap.Remove(inst);
                filled[i] = true;

                // insert placeholder in newSpawned (we'll set actual GameObject after waiting for animations)
                newSpawned.Add(go);
            }
            else
            {
                // placeholder null, will be instantiated later
                newSpawned.Add(null);
            }
        }

        float waitTime = Mathf.Max(0.05f, moveDuration * 0.6f);
        yield return new WaitForSeconds(waitTime);


        // 7) Clean up any leftover visuals that are not used anymore
        // existingMap now contains visuals that were not assigned to any slot -> destroy them
        foreach (var kv in existingMap)
        {
            if (kv.Value != null) Destroy(kv.Value);
        }

        // 8) Rebuild spawnedCards list to match order of owner.hand
        spawnedCards.Clear();
        for (int i = 0; i < newSpawned.Count; i++)
        {
            var go = newSpawned[i];
            if (go != null)
                spawnedCards.Add(go);
        }

        FinalizeTransforms();

        yield break;
    }

    private void FinalizeTransforms()
    {
        // 9) Finalize transforms to exact local positions under handAnchor (snap)
        for (int i = 0; i < spawnedCards.Count; i++)
        {
            var go = spawnedCards[i];
            if (go == null) continue;
            // calculate local target again to avoid tiny float mismatch
            float totalW = Mathf.Max(0, (spawnedCards.Count - 1) * cardSpacing);

            Vector3 localTarget = handAnchor.right * (i * cardSpacing - totalW / 2f)
                + handAnchor.up * Mathf.Sin((i - (spawnedCards.Count - 1) / 2f) * 0.3f) * curveHeight;

            go.transform.SetParent(handAnchor, false);
            go.transform.localPosition = localTarget;
            go.transform.localRotation = Quaternion.Euler(0f, (i - (spawnedCards.Count - 1) / 2f) * 8f, 0f);
        }
    }

    private IEnumerator AnimateInitialHandDraw()
    {
        if (owner == null || owner.hand == null || owner.hand.Count == 0)
            yield break;

        int count = owner.hand.Count;
        float totalWidth = Mathf.Max(0, (count - 1) * cardSpacing);


        soundManager.PlayInitialDraw();

        for (int i = 0; i < count; i++)
        {
            var inst = owner.hand[i];

            // pozycja docelowa (jak w RefreshHand)
            Vector3 targetPos = handAnchor.position
                + handAnchor.right * (i * cardSpacing - totalWidth / 2f)
                + handAnchor.up * Mathf.Sin((i - (count - 1) / 2f) * 0.3f) * curveHeight;
            Quaternion targetRot = handAnchor.rotation * Quaternion.Euler(0, (i - (count - 1) / 2f) * 8f, 0);

            // pozycja startowa — z deckPile lub znad sto³u
            Vector3 spawnPos = handAnchor.position + handAnchor.up * 0.8f + handAnchor.forward * -0.2f;
            Quaternion spawnRot = handAnchor.rotation;

            // jeœli deckPile istnieje — u¿yj go
            var field = GetType().GetField("deckPile");
            if (field != null)
            {
                var dp = field.GetValue(this) as Transform;
                if (dp != null)
                {
                    spawnPos = dp.position;
                    spawnRot = dp.rotation;
                }
            }

            var cardObj = Instantiate(cardPrefab, spawnPos, spawnRot, handAnchor);
            var view = cardObj.GetComponent<CardView>();
            if (view != null)
                view.SetCard(inst);

            // animuj do miejsca w wachlarzu
            StartCoroutine(MoveTransformTo(cardObj.transform, targetPos, targetRot, 0.5f));

            spawnedCards.Add(cardObj);

            yield return new WaitForSeconds(0.1f); // ma³y odstêp miêdzy kartami
        }

        yield return new WaitForSeconds((drawMoveDuration > 0 ? drawMoveDuration : moveDuration) * 0.6f);

    }

    // Helper coroutine to move a transform smoothly to world pos/rot
    protected IEnumerator MoveTransformTo(Transform t, Vector3 worldTargetPos, Quaternion worldTargetRot, float duration)
    {
        if (t == null) yield break;
        Vector3 startPos = t.position;
        Quaternion startRot = t.rotation;
        float elapsed = 0f;
        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float f = Mathf.SmoothStep(0f, 1f, Mathf.Clamp01(elapsed / duration));
            t.position = Vector3.Lerp(startPos, worldTargetPos, f);
            t.rotation = Quaternion.Slerp(startRot, worldTargetRot, f);
            yield return null;
        }
        // ensure final exact
        t.position = worldTargetPos;
        t.rotation = worldTargetRot;
    }

    // publiczna metoda: animuje przeniesienie wizualnej karty odpowiadaj¹cej danej instancji -> slotIndex
    // zwraca true jeœli wizual znaleziono i animacja wystartowa³a
    public virtual bool PlayCardVisualByInstance(CardInstance instance, int slotIndex)
    {
        if (instance == null) return false;
        if (slotTransforms == null || slotIndex < 0 || slotIndex >= slotTransforms.Length) return false;

        GameObject found = null;
        foreach (var go in spawnedCards)
        {
            if (go == null) continue;
            var v = go.GetComponent<CardView>();
            if (v != null && v.model == instance)
            {
                found = go;
                break;
            }
        }

        if (found == null)
        {
            Debug.LogWarning("HandVisualizerBase: nie znaleziono wizualnej karty dla instancji.");
            return false;
        }

        spawnedCards.Remove(found);

        soundManager.PlayPlayCard();

        StartCoroutine(MoveCardToSlotAndFinalize(found, slotTransforms[slotIndex]));
        return true;
    }

    protected virtual IEnumerator MoveCardToSlotAndFinalize(GameObject cardObj, Transform slotTransform)
    {
        float chanceBasic = 0.5f;
        float chanceSineArc = 0.3f;
        float chanceBezierSmoke = 0.2f;
        float total = chanceBasic + chanceSineArc + chanceBezierSmoke;

        float r = UnityEngine.Random.value * total;

        if (r <= chanceBasic)
        {
            yield return StartCoroutine(MoveBasic(cardObj, slotTransform));
        }
        else if (r <= chanceBasic + chanceSineArc)
        {
            yield return StartCoroutine(MoveSineArc(cardObj, slotTransform));
        }
        else
        {
            yield return StartCoroutine(MoveSpiralFlight(cardObj, slotTransform));
        }

        // finalize (jak w Twoim oryginale)
        cardObj.transform.SetParent(slotTransform);
        cardObj.transform.localPosition = new Vector3(0f, 0f, -2.0f);
        cardObj.transform.localRotation = Quaternion.identity;
    } 

    public CardView FindCardViewInSlotsByInstance(CardInstance inst)
    {
        for (int i = 0; i < slotTransforms.Length; i++)
        {
            var v = slotTransforms[i].GetComponentInChildren<CardView>();
            if (v != null && v.model == inst) 
                return v;
        }

        return null;
    }

    [ContextMenu("Refresh Table")]
    public virtual void RefreshTable()
    {
        if (slotTransforms == null || owner == null) return;

        for (int i = 0; i < slotTransforms.Length; i++)
        {
            UpdateSlotVisual(i);
        }
    }

    public virtual void UpdateSlotVisual(int index)
    {
        // szybkie guardy
        if (slotTransforms == null) return;
        if (index < 0 || index >= slotTransforms.Length) return;

        var slot = slotTransforms[index];
        if (slot == null) return;

        CardInstance inst = (owner != null && owner.tableSlots != null && index < owner.tableSlots.Length)
            ? owner.tableSlots[index]
            : null;

        CardView existingView = null;
        if (slot.childCount > 0)
        {
            var child = slot.GetChild(0);
            if (child != null)
                existingView = child.GetComponent<CardView>();
        }

        var manager = FindFirstObjectByType<CardGameLogicManager>();
        List<int> deadSlots = manager != null ? manager.GetDeadSlotsFor(owner) : null;
        bool isDeadSlot = deadSlots != null && deadSlots.Contains(index);

        // 1) jeœli slot jest oznaczony jako "dead" -> spróbuj zagraæ animacjê œmierci
        if (isDeadSlot)
        {
            if (existingView == null)
            {
                // nie ma wizuala do animacji — potwierdz managerowi i koniec
                manager?.AcknowledgeDeath(owner, index);
                return;
            }

            // odpalenie animacji i potwierdzenie managerowi po zakoñczeniu
            soundManager.PlayCardDeath();

            existingView.PlayDeathAnimation(() =>
            {
                if (existingView != null && existingView.gameObject != null)
                    Destroy(existingView.gameObject);

                manager?.AcknowledgeDeath(owner, index);
            });

            return;
        }

        if (inst == null)
        {
            if (existingView != null)
                Destroy(existingView.gameObject);
            return;
        }

        if (existingView == null)
        {
            GameObject cardObj = Instantiate(cardPrefab, slot);
            cardObj.transform.localPosition = new Vector3(0f, 0f, -0.1f);
            cardObj.transform.localRotation = Quaternion.identity;
            cardObj.transform.localScale = Vector3.one;

            existingView = cardObj.GetComponent<CardView>();
        }

        existingView.SetCard(inst);
    }


    public void AnimateDrawToHand(CardInstance instance)
    {
        if (instance == null)
        {
            Debug.LogWarning("AnimateDrawToHand: null instance");
            return;
        }

        // znajdŸ indeks docelowy w rêce (powinien byæ ju¿ dodany w logicznym owner.hand)
        int targetIndex = owner.hand.IndexOf(instance);
        if (targetIndex < 0)
        {
            Debug.LogWarning("AnimateDrawToHand: instance not found in owner.hand");
            return;
        }

        // oblicz docelow¹ pozycjê œwiata (tam gdzie RefreshHand by j¹ umieœci³)
        int count = owner.hand.Count;
        float totalWidth = Mathf.Max(0, (count - 1) * cardSpacing);

        // target local offset wzglêdem handAnchor
        Vector3 localTarget = handAnchor.right * (targetIndex * cardSpacing - totalWidth / 2f)
                            + handAnchor.up * Mathf.Sin((targetIndex - (count - 1) / 2f) * 0.3f) * curveHeight;

        Vector3 worldTargetPos = handAnchor.position + localTarget;
        Quaternion worldTargetRot = handAnchor.rotation * Quaternion.Euler(0, (targetIndex - (count - 1) / 2f) * 8f, 0);

        // utwórz wizual na pozycji deckPile
        GameObject animatedCard = Instantiate(cardPrefab, deckPileTransform.position, deckPileTransform.rotation, this.transform);
        // ustaw skalê/rotacjê tak, by wygl¹da³a prawid³owo

        var view = animatedCard.GetComponent<CardView>();
        if (view != null) view.SetCard(instance);

        // uruchom korutynê animuj¹c¹ ruch z deckPile -> target
        soundManager.PlayDrawCard();

        StartCoroutine(AnimateMoveToHandCoroutine(animatedCard, worldTargetPos, worldTargetRot, targetIndex));
    }

    protected IEnumerator AnimateMoveToHandCoroutine(GameObject cardObj, Vector3 worldTargetPos, Quaternion worldTargetRot, int targetIndex)
    {
        Vector3 startPos = cardObj.transform.position;
        Quaternion startRot = cardObj.transform.rotation;
        float t = 0f;

        while (t < drawMoveDuration)
        {
            t += Time.deltaTime;
            float f = Mathf.SmoothStep(0f, 1f, t / drawMoveDuration);
            cardObj.transform.position = Vector3.Lerp(startPos, worldTargetPos, f);
            cardObj.transform.rotation = Quaternion.Slerp(startRot, worldTargetRot, f);
            yield return null;
        }

        // zakoñcz animacjê: przypnij pod handAnchor i ustaw lokalne transformy
        cardObj.transform.SetParent(handAnchor, true); // zachowaj world pos -> ustawimy lokaln¹ pozycjê
                                                       // ustaw lokalne tak, aby idealnie pasowa³o do layoutu
                                                       // oblicz lokalTarget ponownie, aby byæ pewnym
        int count = owner.hand.Count;
        float totalWidth = Mathf.Max(0, (count - 1) * cardSpacing);
        Vector3 localTarget = handAnchor.right * (targetIndex * cardSpacing - totalWidth / 2f)
                            + handAnchor.up * Mathf.Sin((targetIndex - (count - 1) / 2f) * 0.3f) * curveHeight;

        cardObj.transform.localPosition = localTarget;
        cardObj.transform.localRotation = Quaternion.Euler(0, (targetIndex - (count - 1) / 2f) * 8f, 0);

        // wstaw na odpowiedni¹ pozycjê w spawnedCards
        if (targetIndex <= spawnedCards.Count) spawnedCards.Insert(targetIndex, cardObj);
        else spawnedCards.Add(cardObj);

        FinalizeTransforms();
    }

    protected IEnumerator MoveBasic(GameObject cardObj, Transform slotTransform)
    {
        Vector3 start = cardObj.transform.position;
        Quaternion startR = cardObj.transform.rotation;
        Vector3 end = slotTransform.position;
        Quaternion endR = slotTransform.rotation;

        float t = 0f;
        while (t < moveDuration)
        {
            t += Time.deltaTime;
            float f = Mathf.SmoothStep(0f, 1f, t / moveDuration);
            cardObj.transform.position = Vector3.Lerp(start, end, f);
            cardObj.transform.rotation = Quaternion.Slerp(startR, endR, f);
            yield return null;
        }

        // ensure exact
        cardObj.transform.position = end;
        cardObj.transform.rotation = endR;
    }

    // ---------- wariant 1: Sine-Arc (³adny ³uk z sinusoid¹ w trakcie lotu) ----------
    protected IEnumerator MoveSineArc(GameObject cardObj, Transform slotTransform)
    {
        float sineArcHeight = 0.6f;
        float sineFrequency = 1.8f;

        Vector3 start = cardObj.transform.position;
        Quaternion startR = cardObj.transform.rotation;
        Vector3 end = slotTransform.position;
        Quaternion endR = slotTransform.rotation;

        float t = 0f;
        while (t < moveDuration)
        {
            t += Time.deltaTime;
            float u = Mathf.Clamp01(t / moveDuration);

            // podstawowe eased interpolation
            float eased = Mathf.SmoothStep(0f, 1f, u);

            // liniowa interpolacja pozycji
            Vector3 basePos = Vector3.Lerp(start, end, eased);

            // sinusowy ³uk: sin(w * pi * u) -> 0..1..0 shape, times height
            float arc = Mathf.Sin(Mathf.PI * Mathf.Clamp01(sineFrequency * u)) * sineArcHeight * (1.0f - Mathf.Abs(0.5f - u) * 2f);
            // powy¿sza formu³a da ³adne uniesienie w œrodku lotu; amplitude maleje przy skrajach

            Vector3 pos = basePos + Vector3.up * arc;

            cardObj.transform.position = pos;

            // rotate slowly toward travel direction for natural look
            Vector3 forward = (end - start).normalized;
            if (forward.sqrMagnitude > 0.001f)
            {
                Quaternion targetRot = Quaternion.LookRotation(forward, Vector3.up) * Quaternion.Euler(0f, 180f, 0f); // zale¿nie od orientacji twoich kart, mo¿esz zmodyfikowaæ
                cardObj.transform.rotation = Quaternion.Slerp(startR, endR * targetRot, Mathf.SmoothStep(0f, 1f, u));
            }
            else
            {
                cardObj.transform.rotation = Quaternion.Slerp(startR, endR, Mathf.SmoothStep(0f, 1f, u));
            }

            yield return null;
        }

        // ensure exact
        cardObj.transform.position = end;
        cardObj.transform.rotation = endR;
    }

    // ---------- wariant 2: Bezier + smoke (trajektoria Bezier + spawnowanie dymu) ----------
    protected IEnumerator MoveSpiralFlight(GameObject cardObj, Transform slotTransform)
    {
        // parametry — mo¿esz je te¿ przenieœæ do pól klasy, ¿eby regulowaæ z inspektora
        float moveDuration = 0.6f; // jak d³ugo trwa lot
        float height = 0.6f;       // wysokoœæ ³uku
        float spinSpeed = 1440f;   // prêdkoœæ obrotu wokó³ osi Y (stopnie/sek)
        float wobble = 0.3f;       // ma³y "wê¿yk" w osi X, ¿eby wygl¹da³o nieidealnie

        Vector3 start = cardObj.transform.position;
        Quaternion startR = cardObj.transform.rotation;
        Vector3 end = slotTransform.position;
        Quaternion endR = slotTransform.rotation;

        float t = 0f;
        while (t < moveDuration)
        {
            t += Time.deltaTime;
            float u = Mathf.Clamp01(t / moveDuration);

            // smooth easing
            float eased = Mathf.SmoothStep(0f, 1f, u);

            // podstawowy tor lotu (sinusowy ³uk w górê)
            Vector3 pos = Vector3.Lerp(start, end, eased);
            pos.y += Mathf.Sin(eased * Mathf.PI) * height;

            // lekkie odchylenie w osi X (¿eby tor nie by³ idealnie prosty)
            pos.x += Mathf.Sin(eased * Mathf.PI * 2f) * wobble;

            cardObj.transform.position = pos;

            // obroty: spiralka wokó³ w³asnej osi Y
            float spin = spinSpeed * t; // ci¹g³y obrót
            Quaternion spinRot = Quaternion.Euler(0f, spin, 0f);

            // blend z orientacj¹ koñcow¹ (mo¿esz usun¹æ endR jeœli chcesz ¿eby po prostu stanê³a face-up)
            cardObj.transform.rotation = Quaternion.Slerp(startR, endR, eased) * spinRot;

            yield return null;
        }

        // finalizacja (dok³adnie jak wczeœniej)
        cardObj.transform.position = end;
        cardObj.transform.rotation = endR;
    }
}

