﻿using BannerKings.Managers.Court;
using BannerKings.Managers.Titles;
using TaleWorlds.CampaignSystem;
using TaleWorlds.Core;
using TaleWorlds.Localization;

namespace BannerKings.Models.BKModels
{
    public class BKCouncilModel : IBannerKingsModel
    {
        public ExplainedNumber CalculateEffect(Settlement settlement) => new ExplainedNumber();

        public ExplainedNumber CalculateDemesneLimit(Hero hero)
        {
            ExplainedNumber result = new ExplainedNumber(0.5f, true);
            result.LimitMin(0.5f);
            result.LimitMax(10f);

            result.Add(hero.Clan.Tier / 2f, GameTexts.FindText("str_clan_tier_bonus"));

            FeudalTitle title = BannerKingsConfig.Instance.TitleManager.GetHighestTitle(hero);
            if (title != null)
            {
                float bonus = 0f;
                if (title.type != TitleType.Lordship)
                {
                    if (title.type == TitleType.Barony) bonus = 0.5f;
                    else if (title.type == TitleType.County) bonus = 1f;
                    else if (title.type == TitleType.Dukedom) bonus = 3f;
                    else if (title.type == TitleType.Kingdom) bonus = 6f;
                    else bonus = 10f;
                }

                if (bonus > 0f) result.Add(bonus, new TextObject("Highest title level"));
            }

            return result;
        }

        public ExplainedNumber CalculateVassalLimit(Hero hero)
        {
            ExplainedNumber result = new ExplainedNumber(0f, true);
            result.LimitMin(0f);
            result.LimitMax(50f);
            result.Add(hero.Clan.Tier, GameTexts.FindText("str_clan_tier_bonus"));

            FeudalTitle title = BannerKingsConfig.Instance.TitleManager.GetHighestTitle(hero);
            if (title != null)
            {
                float bonus = 0f;
                if (title.type != TitleType.Lordship)
                {
                    if (title.type == TitleType.Barony) bonus = 1f;
                    else if (title.type == TitleType.County) bonus = 2f;
                    else if (title.type == TitleType.Dukedom) bonus = 4f;
                    else if (title.type == TitleType.Kingdom) bonus = 10f;
                    else bonus = 20f;
                }

                if (bonus > 0f) result.Add(bonus, new TextObject("Highest title level"));
            }

            return result;
        }

        public (bool, string) IsCouncilRoyal(Clan clan)
        {
            TextObject explanation = new TextObject("{=!}Legal crown council.");

            Kingdom kingdom = clan.Kingdom;
            if (kingdom == null) 
            {
                explanation = new TextObject("{=!}No kingdom.");
                return new(false, explanation.ToString());
            }

            if (clan.Kingdom.RulingClan != clan)
            {
                explanation = new TextObject("{=!}Not the ruling clan.");
                return new(false, explanation.ToString());
            }

            FeudalTitle sovereign = BannerKingsConfig.Instance.TitleManager.GetSovereignTitle(kingdom);
            if (sovereign == null)
            {
                explanation = new TextObject("{=!}Does not hold faction's sovereign title.");
                return new(false, explanation.ToString());
            }

            return new(true, explanation.ToString());
        }
        
        public bool WillAcceptAction(CouncilAction action, Hero hero)
        {
            if (action.Type != CouncilActionType.REQUEST) return true;
            
            return action.Possible;
        }


        public CouncilAction GetAction(CouncilActionType type, CouncilData council, Hero requester, CouncilMember targetPosition, CouncilMember currentPosition = null,
            bool appointed = false)
        {
            if (type == CouncilActionType.REQUEST)
                return GetRequest(type, council, requester, targetPosition, currentPosition, appointed);
            else if (type == CouncilActionType.RELINQUISH)
                return GetRelinquish(type, council, requester, currentPosition, targetPosition, appointed);
            else return GetSwap(type, council, requester, targetPosition, currentPosition, appointed);
        }


        private CouncilAction GetSwap(CouncilActionType type, CouncilData council, Hero requester, CouncilMember targetPosition, CouncilMember currentPosition = null, bool appointed = false)
        {
            CouncilAction action = new CouncilAction(type, requester, targetPosition, currentPosition, council);
            action.Influence = GetInfluenceCost(type, targetPosition);

            if (currentPosition == null || currentPosition.Member != requester)
            {
                action.Possible = false;
                action.Reason = new TextObject("{=!}Not part of the council.");
                return action;
            }

            if (!targetPosition.IsValidCandidate(requester))
            {
                action.Possible = false;
                action.Reason = new TextObject("{=!}Not a valid candidate.");
                return action;
            }

            if (requester.Clan != null && requester.Clan.Influence < action.Influence)
            {
                action.Possible = false;
                action.Reason = new TextObject("{=!}Not enough influence.");
                return action;
            }

            if (targetPosition.IsCorePosition(targetPosition.Position))
            {
                if (requester.Clan != null && !requester.Clan.Kingdom.Leader.IsFriend(requester))
                {
                    action.Possible = false;
                    action.Reason = new TextObject("{=!}Not trustworthy enough for this position.");
                    return action;
                }

                if (council.GetCompetence(requester, targetPosition.Position) < 0.5f)
                {
                    action.Possible = false;
                    action.Reason = new TextObject("{=!}Not competent enough for this position.");
                    return action;
                }
            }

            if (targetPosition.Member != null)
            {
                float candidateDesire = GetDesirability(requester, council, targetPosition);
                float currentDesire = GetDesirability(targetPosition.Member, council, targetPosition);
                if (currentDesire > candidateDesire)
                {
                    action.Possible = false;
                    action.Reason = new TextObject("{=!}Not a better candidate than current councillor.");
                    return action;
                }
            }

            action.Possible = true;
            action.Reason = new TextObject("{=!}Action can be taken.");
            return action;
        }

        private CouncilAction GetRelinquish(CouncilActionType type, CouncilData council, Hero requester, CouncilMember currentPosition, CouncilMember targetPosition = null, bool appointed = false)
        {
            CouncilAction action = new CouncilAction(type, requester, targetPosition, currentPosition, council);
            action.Influence = GetInfluenceCost(type, targetPosition);

            if (requester != null)
            {
                if (targetPosition == null)
                {
                    action.Possible = false;
                    action.Reason = new TextObject("{=!}No position to be relinquished.");
                    return action;
                }

                if (targetPosition.Member != requester)
                {
                    action.Possible = false;
                    action.Reason = new TextObject("{=!}Not current councilman of the position.");
                    return action;
                }
            }

            action.Possible = true;
            action.Reason = new TextObject("{=!}Action can be taken.");
            return action;
        }

        private CouncilAction GetRequest(CouncilActionType type, CouncilData council, Hero requester, CouncilMember targetPosition, CouncilMember currentPosition = null, bool appointed = false)
        {
            CouncilAction action = new CouncilAction(type, requester, targetPosition, currentPosition, council);
            action.Influence = appointed ? 0f : GetInfluenceCost(type, targetPosition);

            if (currentPosition != null && currentPosition.Member == requester)
            {
                action.Possible = false;
                action.Reason = new TextObject("{=!}Already part of the council.");
                return action;
            }

            if (!targetPosition.IsValidCandidate(requester))
            {
                action.Possible = false;
                action.Reason = new TextObject("{=!}Not a valid candidate.");
                return action;
            }

            if (requester.Clan != null && requester.Clan.Influence < action.Influence)
            {
                action.Possible = false;
                action.Reason = new TextObject("{=!}Not enough influence.");
                return action;
            }


            if (!appointed)
            {
                if (targetPosition.IsCorePosition(targetPosition.Position))
                {
                    if (requester.Clan != null && !requester.Clan.Kingdom.Leader.IsFriend(requester))
                    {
                        action.Possible = false;
                        action.Reason = new TextObject("{=!}Not trustworthy enough for this position.");
                        return action;
                    }

                    if (council.GetCompetence(requester, targetPosition.Position) < 0.5f)
                    {
                        action.Possible = false;
                        action.Reason = new TextObject("{=!}Not competent enough for this position.");
                        return action;
                    }
                }

                if (targetPosition.Member != null)
                {
                    float candidateDesire = GetDesirability(requester, council, targetPosition);
                    float currentDesire = GetDesirability(targetPosition.Member, council, targetPosition);
                    if (currentDesire > candidateDesire)
                    {
                        action.Possible = false;
                        action.Reason = new TextObject("{=!}Not a better candidate than current councillor.");
                        return action;
                    }
                }
            }

            action.Possible = true;
            action.Reason = new TextObject("{=!}Action can be taken.");
            return action;
        }

        public float GetDesirability(Hero candidate, CouncilData council, CouncilMember position)
        {
            float titleWeight = 0;
            float competence = council.GetCompetence(candidate, position.Position);
            float relation = council.Owner.GetRelation(candidate) * 0.01f;
            if (candidate.Clan == council.Owner.Clan)
                relation -= 0.2f;
            FeudalTitle title = BannerKingsConfig.Instance.TitleManager.GetHighestTitle(candidate);
            if (title != null)
                titleWeight = 4 - (int)title.type;

            return (titleWeight + competence + relation) / 3f;
        }

        public int GetInfluenceCost(CouncilActionType type, CouncilMember targetPosition)
        {
            if (type == CouncilActionType.REQUEST)
            {
                if (targetPosition.Member != null)
                    return 100;
                else return 50;
            }
            else if (type == CouncilActionType.RELINQUISH)
                return 0;
            else
            {
                if (targetPosition.Member != null)
                    return 50;
                return 10;
            }
        }
    }
}
