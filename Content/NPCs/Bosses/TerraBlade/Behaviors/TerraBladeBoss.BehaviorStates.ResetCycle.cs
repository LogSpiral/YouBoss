using System.Collections.Generic;
using System.Linq;
using YouBoss.Common.Tools.Reflection;
using Terraria;
using Terraria.Utilities;

namespace YouBoss.Content.NPCs.Bosses.TerraBlade
{
    public partial class TerraBladeBoss
    {
        /// <summary>
        /// The previous two states the terra blade performed.
        /// </summary>
        public TerraBladeAIType[] PreviousTwoStates
        {
            get;
            set;
        } = new TerraBladeAIType[2];

        /// <summary>
        /// The previous state the terra blade performed.
        /// </summary>
        public TerraBladeAIType PreviousState => PreviousTwoStates[^1];

        [AutomatedMethodInvoke]
        public void LoadState_ResetCycle()
        {
            StateMachine.RegisterTransition(TerraBladeAIType.ResetCycle, null, false, () => true,
                () =>
                {
                    // Clear the state stack.
                    StateMachine.StateStack.Clear();

                    // Choose an attack pattern to use.
                    TerraBladeAIType[] attackPattern = ChooseNextAttackPattern();
                    PreviousTwoStates[^2] = PreviousTwoStates[^1];
                    PreviousTwoStates[^1] = attackPattern[^1];

                    // Supply the state stack with the attack pattern.
                    for (int i = attackPattern.Length - 1; i >= 0; i--)
                        StateMachine.StateStack.Push(StateMachine.StateRegistry[attackPattern[i]]);
                });
        }

        public TerraBladeAIType[] ChooseNextAttackPattern()
        {
            // Initialize the attack RNG.
            WeightedRandom<TerraBladeAIType> rng = new(Main.rand);
            rng.Add(TerraBladeAIType.RapidDashes, CalculateRapidDashesAttackWeight());
            rng.Add(TerraBladeAIType.SingleSwipe, CalculateSingleSwipeAttackWeight());
            rng.Add(TerraBladeAIType.EnergyBeamSpin, CalculateEnergyBeamSpinAttackWeight());
            rng.Add(TerraBladeAIType.MorphoKnightLungeSweeps, CalculateMorphoKnightLungeSweepsAttackWeight());
            rng.Add(TerraBladeAIType.DashSpin, CalculateDashSpinAttackWeight());
            rng.Add(TerraBladeAIType.AcceleratingBeamWall, CalculateAcceleratingBeamWallAttackWeight());

            if (Phase2)
            {
                rng.Add(TerraBladeAIType.DiamondSweeps, CalculateDiamondSweepsAttackWeight());
                rng.Add(TerraBladeAIType.TelegraphedBeamDashes, CalculateTelegraphedBeamDashesAttackWeight());
            }

            if (Phase3)
            {
                rng.Add(TerraBladeAIType.BreakIntoTrueBlades, CalculateBreakIntoTrueBladesAttackWeight());
                rng.Add(TerraBladeAIType.AerialSwoopDashes, CalculateAerialSwoopDashesAttackWeight());
            }

            // Attempt to find a suitable attack to use, discarding attacks if the previous two states match the current state.
            int tries = 0;
            TerraBladeAIType candidate;
            do
            {
                candidate = rng;
                tries++;
                if (tries >= 400)
                    break;
            }
            while (PreviousTwoStates.Contains(candidate));

            List<TerraBladeAIType> pattern =
            [
                candidate
            ];
            if (candidate == TerraBladeAIType.SingleSwipe && PreviousState != TerraBladeAIType.RapidDashes)
                pattern.Insert(0, TerraBladeAIType.RapidDashes);

            return [.. pattern];
        }
    }
}
