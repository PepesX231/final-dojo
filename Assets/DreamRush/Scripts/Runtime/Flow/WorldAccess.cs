using DreamRush.Core;
using DreamRush.Campaign;
namespace DreamRush.Flow
{
    public static class WorldAccess
    {
        public static bool CanEnter(string scene,bool authoredLocked,ProgressionData progress)
        {
            if(authoredLocked||string.IsNullOrEmpty(scene))return false;
            int index=CampaignRoute.Index(scene);
            for(int i=0;i<index;i++)if((progress?.FindLevelRecord(CampaignRoute.Scenes[i])?.clears??0)<=0)return false;
            return true;
        }
        public static string Reason(string scene,bool authoredLocked)
        {
            int index=CampaignRoute.Index(scene);
            return !authoredLocked&&index>0?"ชนะ "+CampaignRoute.Titles[index-1]+" (WORLD "+index+") ก่อน เพื่อเปิดโลกถัดไป":"โลกนี้ยังอยู่ระหว่างสร้าง";
        }
        public static string LockLabel(string scene,bool authoredLocked)
        {
            int index=CampaignRoute.Index(scene);return !authoredLocked&&index>0?"WIN WORLD "+index:"LOCKED";
        }
    }
}
