namespace TRPGHelper.Domain.Enums;

/// <summary>
/// 卡的操作流水，用来回答“我车过什么、什么时候留了卡”。
/// </summary>
public enum CardAction
{
    Created = 0,
    Updated = 1,
    StatusChanged = 2,
    Retained = 3,
    Released = 4,
    Archived = 5,
    Rolled = 6,
    Note = 7,
}

public static class CardActionExtensions
{
    public static string ToLabel(this CardAction action) => action switch
    {
        CardAction.Created => "创建角色卡",
        CardAction.Updated => "更新资料",
        CardAction.StatusChanged => "状态变更",
        CardAction.Retained => "留卡",
        CardAction.Released => "取出留存",
        CardAction.Archived => "归档",
        CardAction.Rolled => "掷骰 / 车卡",
        CardAction.Note => "备注",
        _ => action.ToString(),
    };

    public static string ToKey(this CardAction action) => action switch
    {
        CardAction.Created => "created",
        CardAction.Updated => "updated",
        CardAction.StatusChanged => "statusChanged",
        CardAction.Retained => "retained",
        CardAction.Released => "released",
        CardAction.Archived => "archived",
        CardAction.Rolled => "rolled",
        CardAction.Note => "note",
        _ => action.ToString().ToLowerInvariant(),
    };
}
