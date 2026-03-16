using UnityEngine;

public static class ShopOverlayPanel
{
    public static void Draw(RunManager runManager)
    {
        Rect area = new Rect(Mathf.Max(20f, (Screen.width - 720f) * 0.5f), Mathf.Max(20f, (Screen.height - 320f) * 0.5f), Mathf.Min(720f, Screen.width - 40f), 280f);
        GUILayout.BeginArea(area, GUI.skin.window);
        GUILayout.Label("Botiga de la run");
        GUILayout.Label($"Or disponible: {runManager.CurrentGold}");

        for (int i = 0; i < runManager.CurrentShopOffers.Count; i++)
        {
            ShopOfferData offer = runManager.CurrentShopOffers[i];
            string label = $"{offer.title} ({offer.cost} or)\n{offer.description}";
            if (GUILayout.Button(label, GUILayout.Height(54f)))
                runManager.BuyShopOffer(i);
        }

        GUILayout.Space(10f);
        if (GUILayout.Button("Continuar sense comprar", GUILayout.Height(32f)))
            runManager.SkipShop();

        GUILayout.EndArea();
    }
}
