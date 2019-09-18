public partial struct Skill
{
    // wont show up in the skill window for learning/upgrade - only works with UCE skill window addon
    public bool unlearnable { get { return data.unlearnable; } }

    // is considered to be a negative status effect and can be removed by certain skills
    public bool disadvantageous { get { return data.disadvantageous; } }
}