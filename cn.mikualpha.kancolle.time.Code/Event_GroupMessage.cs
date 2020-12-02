using Native.Sdk.Cqp.EventArgs;
using Native.Sdk.Cqp.Interface;

public class Event_GroupMessage : IGroupMessage
{
    #region --公开方法--
    /// <summary>
    /// Type=2 群消息<para/>
    /// 处理收到的群消息
    /// </summary>
    /// <param name="sender">事件的触发对象</param>
    /// <param name="e">事件的附加参数</param>
    public void GroupMessage(object sender, CQGroupMessageEventArgs e)
    {
        if (e.IsFromAnonymous)    //如果此属性不为null, 则消息来自于匿名成员
        {
            e.Handler = false;
            return;
        }

        if (TimeCheck.isAdmin(e.FromQQ.Id))
        {
            if (e.Message.Text.Contains("/启用报时-"))
            {
                string name = e.Message.Text.Substring(e.Message.Text.IndexOf("-") + 1);
                char[] checkWords = { '[', ']', ',', '，', '。', '!', '！', '/', '\\', '（', '(', ')', '）' };
                if (name.IndexOfAny(checkWords) != -1)
                {
                    e.FromGroup.SendGroupMessage("非法的API请求，请勿滥用指令！");
                }
                else if (TimeCheck.GetInstance(e.FromGroup.Id).StartCheck(name))
                {
                    e.FromGroup.SendGroupMessage("已成功将[" + name + "]设为报时秘书舰！");
                }
                else
                {
                    e.FromGroup.SendGroupMessage("获取报时数据失败！");
                }
                e.Handler = true;
            }
                 
            if (e.Message.Text.Equals("/禁用报时"))
            {
                if (TimeCheck.GetInstance(e.FromGroup.Id).EndCheck())
                {
                    e.FromGroup.SendGroupMessage("已成功禁用报时！");
                }
                else
                {
                    e.FromGroup.SendGroupMessage("目前本群没有处于启用状态的报时！");
                }
                e.Handler = true;
            }
        }
    }
    #endregion
}

