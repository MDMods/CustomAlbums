namespace CustomAlbums.Data
{
    internal class BmsStates
    {
        public enum AnimAlignment
        {
            Left = -1,
            Right = 1
        }

        public enum BossState
        {
            OffScreen,
            Idle,
            Phase1,
            Phase2
        }

        public static readonly Dictionary<string, BossState> AnimStatesLeft = new()
        {

            { "in", BossState.OffScreen },
            { "out", BossState.Idle },
            { "boss_close_atk_1", BossState.Idle },
            { "boss_close_atk_2", BossState.Idle },
            { "multi_atk_48", BossState.Idle },
            { "multi_atk_48_end", BossState.Idle },
            { "boss_far_atk_1_L", BossState.Phase1 },
            { "boss_far_atk_1_R", BossState.Phase1 },
            { "boss_far_atk_2", BossState.Phase2 },
            { "boss_far_atk_1_start", BossState.Idle },
            { "boss_far_atk_2_start", BossState.Idle },
            { "boss_far_atk_1_end", BossState.Phase1 },
            { "boss_far_atk_2_end", BossState.Phase2 },
            { "atk_1_to_2", BossState.Phase1 },
            { "atk_2_to_1", BossState.Phase2 }
        };

        public static readonly Dictionary<string, BossState> AnimStatesRight = new()
        {
            { "in", BossState.Idle },
            { "out", BossState.OffScreen },
            { "boss_close_atk_1", BossState.Idle },
            { "boss_close_atk_2", BossState.Idle },
            { "multi_atk_48", BossState.Idle },
            { "multi_atk_48_end", BossState.OffScreen },
            { "boss_far_atk_1_L", BossState.Phase1 },
            { "boss_far_atk_1_R", BossState.Phase1 },
            { "boss_far_atk_2", BossState.Phase2 },
            { "boss_far_atk_1_start", BossState.Phase1 },
            { "boss_far_atk_2_start", BossState.Phase2 },
            { "boss_far_atk_1_end", BossState.Idle },
            { "boss_far_atk_2_end", BossState.Idle },
            { "atk_1_to_2", BossState.Phase2 },
            { "atk_2_to_1", BossState.Phase1 }
        };

        public static readonly Dictionary<BossState, Dictionary<BossState, string>> StateTransferAnims = new()
        {
            {
                BossState.OffScreen, new Dictionary<BossState, string>
                {
                    { BossState.OffScreen, "0" },
                    { BossState.Idle, "in" },
                    { BossState.Phase1, "boss_far_atk_1_start" },
                    { BossState.Phase2, "boss_far_atk_2_start" }
                }
            },
            {
                BossState.Idle, new Dictionary<BossState, string>
                {
                    { BossState.OffScreen, "out" },
                    { BossState.Idle, "0" },
                    { BossState.Phase1, "boss_far_atk_1_start" },
                    { BossState.Phase2, "boss_far_atk_2_start" }
                }
            },
            {
                BossState.Phase1, new Dictionary<BossState, string>
                {
                    { BossState.OffScreen, "out" },
                    { BossState.Idle, "boss_far_atk_1_end" },
                    { BossState.Phase1, "0" },
                    { BossState.Phase2, "atk_1_to_2" }
                }
            },
            {
                BossState.Phase2, new Dictionary<BossState, string>
                {
                    { BossState.OffScreen, "out" },
                    { BossState.Idle, "boss_far_atk_2_end" },
                    { BossState.Phase1, "atk_2_to_1" },
                    { BossState.Phase2, "0" }
                }
            }
        };

        public static readonly Dictionary<string, AnimAlignment> TransferAlignment = new()
        {
            { "in", AnimAlignment.Right },
            { "out", AnimAlignment.Left },
            { "boss_far_atk_1_start", AnimAlignment.Right },
            { "boss_far_atk_2_start", AnimAlignment.Right },
            { "boss_far_atk_1_end", AnimAlignment.Left },
            { "boss_far_atk_2_end", AnimAlignment.Left },
            { "atk_1_to_2", AnimAlignment.Right },
            { "atk_2_to_1", AnimAlignment.Right }
        };
    }
}