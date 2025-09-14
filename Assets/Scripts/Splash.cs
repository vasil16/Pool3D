using UnityEngine;
using UnityEngine.UI;
using DG.Tweening;
using System.Collections;

public class Splash : MonoBehaviour
{
    [Header("UI References")]
    public GameObject splashPanel;   // Assign your splash panel
    public Image logoImage;          // Optional: your logo
    public Slider progressBar;       // Optional: progress bar

    [Header("Settings")]
    public float minSplashTime = 0.5f;   // Minimum time splash stays visible

    private void Start()
    {
        StartCoroutine(HandleSplash());
    }

    private IEnumerator HandleSplash()
    {
        splashPanel.SetActive(true);

        // Fade in logo if assigned
        if (logoImage != null)
        {
            logoImage.color = new Color(1, 1, 1, 0);
            logoImage.DOFade(1f, 0.5f);
        }

        // Run initialization tasks with smooth progress
        yield return StartCoroutine(DoInitialization());

        // Ensure minimum splash duration
        yield return new WaitForSeconds(minSplashTime);

        // Fade out splash panel smoothly
        CanvasGroup cg = splashPanel.GetComponent<CanvasGroup>();
        if (cg == null)
        {
            cg = splashPanel.AddComponent<CanvasGroup>();
        }

        cg.DOFade(0f, 0.6f).OnComplete(() =>
        {
            splashPanel.SetActive(false);
        });
    }

    private IEnumerator DoInitialization()
    {
        if (progressBar != null)
            progressBar.value = 0f;

        // --- Task 1: Audio warmup ---
        AudioSource[] sources = FindObjectsOfType<AudioSource>(true);
        foreach (var src in sources)
        {
            if (src.clip != null) { src.Play(); src.Pause(); }
        }
        if (progressBar != null)
            progressBar.DOValue(0.33f, 0.2f);  // smooth to 33%
        yield return new WaitForSeconds(0.2f);

        // --- Task 2: Lighting environment ---
        LightProbes.Tetrahedralize();
        DynamicGI.UpdateEnvironment();
        if (progressBar != null)
            progressBar.DOValue(0.66f, 0.2f);  // smooth to 66%
        yield return new WaitForSeconds(0.2f);

        // --- Task 3: GC cleanup ---
        System.GC.Collect();
        if (progressBar != null)
            progressBar.DOValue(1f, 0.2f);  // smooth to 100%
        yield return new WaitForSeconds(0.2f);
    }
}
