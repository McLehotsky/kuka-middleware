using KukaMiddleware.Api.Missions;

namespace KukaMiddleware.Api.Tests.Missions;

public class MissionValidationTests
{
    private static readonly CreateMissionRequest Valid = new("RACK_MOVE", null, null, null, [new("N-01", "Load")]);

    private static CreateMissionRequest WithSecondStep(string? nodeCode, string? action) =>
        Valid with { Steps = [new("N-01", "Load"), new(nodeCode, action)] };

    [Fact]
    public void Valid_request_has_no_errors() => Assert.Empty(MissionValidation.Validate(Valid));

    [Theory]
    [InlineData("RACK_MOVE")]
    [InlineData("ROLLER_MOVE")]
    public void Known_mission_type_is_valid(string missionType) =>
        Assert.Empty(MissionValidation.Validate(Valid with { MissionType = missionType }));

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("FLY")]
    [InlineData("rack_move")]
    public void Missing_or_unknown_mission_type_is_invalid(string? missionType) =>
        Assert.Contains("missionType", MissionValidation.Validate(Valid with { MissionType = missionType }).Keys);

    [Theory]
    [InlineData(null)]
    [InlineData(1)]
    [InlineData(2)]
    [InlineData(3)]
    public void Priority_1_to_3_or_omitted_is_valid(int? priority) =>
        Assert.Empty(MissionValidation.Validate(Valid with { Priority = priority }));

    [Theory]
    [InlineData(0)]
    [InlineData(4)]
    [InlineData(-1)]
    public void Priority_outside_1_to_3_is_invalid(int priority) =>
        Assert.Contains("priority", MissionValidation.Validate(Valid with { Priority = priority }).Keys);

    [Fact]
    public void Missing_steps_are_invalid() =>
        Assert.Contains("steps", MissionValidation.Validate(Valid with { Steps = null }).Keys);

    [Fact]
    public void Zero_steps_are_invalid() =>
        Assert.Contains("steps", MissionValidation.Validate(Valid with { Steps = [] }).Keys);

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void Blank_node_code_is_invalid_with_step_index_key(string? nodeCode) =>
        Assert.Contains("steps[1].nodeCode", MissionValidation.Validate(WithSecondStep(nodeCode, "Load")).Keys);

    [Fact]
    public void Node_code_of_64_characters_is_valid() =>
        Assert.Empty(MissionValidation.Validate(WithSecondStep(new string('N', 64), "Load")));

    [Fact]
    public void Node_code_of_65_characters_is_invalid() =>
        Assert.Contains("steps[1].nodeCode", MissionValidation.Validate(WithSecondStep(new string('N', 65), "Load")).Keys);

    [Theory]
    [InlineData("Load")]
    [InlineData("Unload")]
    [InlineData("Wait")]
    public void Known_action_is_valid(string action) =>
        Assert.Empty(MissionValidation.Validate(WithSecondStep("N-02", action)));

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("Jump")]
    [InlineData("load")]
    public void Missing_or_unknown_action_is_invalid_with_step_index_key(string? action) =>
        Assert.Contains("steps[1].action", MissionValidation.Validate(WithSecondStep("N-02", action)).Keys);

    [Fact]
    public void External_ref_of_100_characters_is_valid() =>
        Assert.Empty(MissionValidation.Validate(Valid with { ExternalRef = new string('E', 100) }));

    [Fact]
    public void External_ref_of_101_characters_is_invalid() =>
        Assert.Contains("externalRef", MissionValidation.Validate(Valid with { ExternalRef = new string('E', 101) }).Keys);
}
