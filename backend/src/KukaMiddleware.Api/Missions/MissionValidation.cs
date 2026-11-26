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
        else if (request.MissionType == "ROLLER_MOVE")
            // KUKA FM needs a binCode per step that the API does not have yet (system-definition §14 Phase 5 scope).
            errors["missionType"] = ["ROLLER_MOVE is not supported yet."];

        if (request.Priority is < 1 or > 3)
            errors["priority"] = ["Must be between 1 and 3."];

        if (request.ExternalRef?.Length > 100)
            errors["externalRef"] = ["Must be at most 100 characters."];

        // KUKA FM rejects a mission with fewer than 2 missionData entries (observed live 2026-11-26).
        if (request.Steps is not { Count: >= 2 } steps)
        {
            errors["steps"] = ["At least two steps are required."];
            return errors;
        }

        for (var i = 0; i < steps.Count; i++)
        {
            var step = steps[i];
            if (step is null)
            {
                errors[$"steps[{i}].nodeCode"] = ["A step is required."];
                continue;
            }

            if (string.IsNullOrWhiteSpace(step.NodeCode) || step.NodeCode!.Length > 64)
                errors[$"steps[{i}].nodeCode"] = ["Must be 1 to 64 characters and not blank."];

            if (!Actions.Contains(step.Action))
                errors[$"steps[{i}].action"] = [$"Must be one of: {string.Join(", ", Actions)}."];
        }

        return errors;
    }
}
