using System;
namespace DreamRush.Campaign
{
    public static class CampaignRoute
    {
        public static readonly string[] Scenes={"Dream_World1","Dream_MidnightPalace","Dream_ToyBoxMoon","Dream_BubbleBathSea","Dream_MidnightTeacup"};
        public static readonly string[] Titles={"THE DREAM BOOK","MIDNIGHT PALACE","TOY BOX MOON","BUBBLE BATH SEA","MIDNIGHT TEACUP"};
        public static int Index(string scene)=>Array.IndexOf(Scenes,scene);
        public static string BossId(string scene)=>scene=="Dream_MidnightPalace"||scene=="Test_BossMidnight"?"CountMidnight":scene=="Dream_ToyBoxMoon"?"LullabyMoon":scene=="Dream_BubbleBathSea"?"StormOfTheBath":scene=="Dream_MidnightTeacup"?"TheLastKnock":"Chair";
    }
}
