namespace KukaMiddleware.Api.Missions;

// Shape checks of CreateMissionRequest (system-definition §8). Keys follow the JSON field names,
// steps are indexed from 0. Robot existence needs the database and is checked by MissionService.
public static class MissionValidation
{
    private static readonly string[] MissionTypes = ["RACK_MOVE", "ROLLER_MOVE"];
    private static readonly string[] Actions = ["Load", "Unload", "Wait"];

    public static Dictionary<string, string[]> Validate(CreateMissionRequest request)
    {
        var errors = new Dictionary<string, string[]>();

        if (!MissionTypes.Contains(request.MissionType))
            errors["missionType"] = [$"Must be one of: {string.Join(", ", MissionTypes)}."];

        if (request.Priority is < 1 or > 3)
            errors["priority"] = ["Must be between 1 and 3."];

        if (request.ExternalRef?.Length > 100)
            errors["externalRef"] = ["Must be at most 100 characters."];

        if (request.Steps is not { Count: > 0 } steps)
        {
            errors["steps"] = ["At least one step is required."];
            return errors;
        }

        for (var i = 0; i < steps.Count; i++)
        {
            if (string.IsNullOrWhiteSpace(steps[i].NodeCode) || steps[i].NodeCode!.Length > 64)
                errors[$"steps[{i}].nodeCode"] = ["Must be 1 to 64 characters and not blank."];

            if (!Actions.Contains(steps[i].Action))
                errors[$"steps[{i}].action"] = [$"Must be one of: {string.Join(", ", Actions)}."];
        }

        return errors;
    }
}
