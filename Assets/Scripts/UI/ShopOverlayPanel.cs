using System.Collections.Generic;
using UnityEngine;

public static class ShopOverlayPanel
{
    private static readonly Dictionary<int, Vector2> ScrollPositions = new Dictionary<int, Vector2>();

    public static void Draw(RunManager runManager)
    {
        ShopOfferData[] offers = SnapshotOffers(runManager);
        float width = Mathf.Min(1180f, Screen.width - 36f);
        float height = Mathf.Min(390f, Screen.height - 40f);
        Rect area = new Rect((Screen.width - width) * 0.5f, Mathf.Max(20f, (Screen.height - height) * 0.5f), width, height);
        RunUiTheme.DrawPanel(area, new Color32(28, 23, 18, 244), new Color32(206, 170, 98, 255));

        GUI.Label(new Rect(area.x + 24f, area.y + 18f, area.width - 48f, 34f), "Botiga de la run", RunUiTheme.TitleStyle);
        RunUiTheme.DrawBadge(new Rect(area.x + area.width - 200f, area.y + 20f, 176f, 30f), $"Or disponible: {runManager.CurrentGold}", new Color32(223, 190, 113, 255), new Color32(22, 18, 14, 255));
        GUI.Label(new Rect(area.x + 24f, area.y + 58f, area.width - 48f, 28f), "Compra buffs, cura o utilitats abans del proper segment. Cada targeta mostra les dades principals de l'oferta activa.", RunUiTheme.BodyStyle);
        RunUiTheme.DrawDivider(new Rect(area.x + 24f, area.y + 92f, area.width - 48f, 2f), new Color32(126, 100, 69, 255));

        int count = Mathf.Max(1, offers.Length);
        float gap = 16f;
        float cardWidth = (area.width - 48f - gap * (count - 1)) / count;
        float cardHeight = 220f;

        for (int i = 0; i < offers.Length; i++)
        {
            Rect cardRect = new Rect(area.x + 24f + (cardWidth + gap) * i, area.y + 110f, cardWidth, cardHeight);
            if (DrawOffer(cardRect, offers[i], i, runManager))
                return;
        }

        Rect skipRect = new Rect(area.x + 24f, area.y + area.height - 48f, area.width - 48f, 34f);
        RunUiTheme.DrawButtonBackground(skipRect, new Color32(124, 130, 136, 255));
        if (GUI.Button(skipRect, "Continuar sense comprar", RunUiTheme.PrimaryButtonStyle))
            runManager.SkipShop();
    }

    private static bool DrawOffer(Rect rect, ShopOfferData offer, int index, RunManager runManager)
    {
        RunUiTheme.DrawPanel(rect, new Color32(47, 38, 30, 255), new Color32(184, 149, 90, 255));
        Rect inner = new Rect(rect.x + 16f, rect.y + 16f, rect.width - 32f, rect.height - 32f);
        GUI.BeginGroup(inner);
        GUI.Label(new Rect(0f, 0f, inner.width, 28f), offer.title, RunUiTheme.SubtitleStyle);
        GUI.Label(new Rect(0f, 26f, inner.width, 20f), $"ID: {offer.offerId}   |   Tipus: {offer.offerType}", RunUiTheme.MutedStyle);
        RunUiTheme.DrawBadge(new Rect(0f, 54f, 110f, 28f), $"{offer.cost} or", new Color32(223, 190, 113, 255), new Color32(20, 16, 12, 255));

        Rect scrollRect = new Rect(0f, 90f, inner.width, inner.height - 144f);
        Rect viewRect = new Rect(0f, 0f, inner.width - 18f, 140f);
        Vector2 scroll = ScrollPositions.TryGetValue(index, out Vector2 existing) ? existing : Vector2.zero;
        scroll = GUI.BeginScrollView(scrollRect, scroll, viewRect, false, true);
        ScrollPositions[index] = scroll;
        GUI.Label(new Rect(0f, 0f, viewRect.width, 60f), offer.description, RunUiTheme.BodyStyle);
        GUI.Label(new Rect(0f, 64f, viewRect.width, 20f), $"Reward id: {offer.rewardId}", RunUiTheme.MutedStyle);
        GUI.Label(new Rect(0f, 86f, viewRect.width, 20f), $"Quantitat: {offer.quantity}   |   Valor: {offer.value:0.##}", RunUiTheme.MutedStyle);
        GUI.EndScrollView();

        Rect buyRect = new Rect(0f, inner.height - 42f, inner.width, 42f);
        RunUiTheme.DrawButtonBackground(buyRect, runManager.CurrentGold >= offer.cost ? new Color32(216, 186, 108, 255) : new Color32(122, 116, 110, 255));
        bool purchased = GUI.Button(buyRect, runManager.CurrentGold >= offer.cost ? $"Comprar opcio {index + 1}" : $"Falten {offer.cost - runManager.CurrentGold} monedes", RunUiTheme.PrimaryButtonStyle);
        GUI.EndGroup();

        if (purchased)
        {
            runManager.BuyShopOffer(index);
            return true;
        }

        return false;
    }

    private static ShopOfferData[] SnapshotOffers(RunManager runManager)
    {
        int count = runManager.CurrentShopOffers.Count;
        ShopOfferData[] snapshot = new ShopOfferData[count];
        for (int i = 0; i < count; i++)
            snapshot[i] = runManager.CurrentShopOffers[i];
        return snapshot;
    }
}
