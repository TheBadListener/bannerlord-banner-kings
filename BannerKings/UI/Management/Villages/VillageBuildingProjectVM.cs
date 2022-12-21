﻿using System;
using TaleWorlds.CampaignSystem.Settlements.Buildings;
using TaleWorlds.CampaignSystem.ViewModelCollection.GameMenu.TownManagement;

namespace BannerKings.UI.Management.Villages
{
    public class VillageBuildingProjectVM : SettlementBuildingProjectVM
    {
        public VillageBuildingProjectVM(Action<SettlementProjectVM, bool> onSelection, Action<SettlementProjectVM> onSetAsCurrent, Action onResetCurrent, Building building) : base(onSelection, onSetAsCurrent, onResetCurrent, building)
        {
        }

        public override void RefreshProductionText()
        {
        }
    }
}
