using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using System.Text;
using isRock.LineBot;

namespace main.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class LineBotController : ControllerBase
    {
        public delegate string DoEvent(string Message, ref List<MessageBase> response);
        class EVENT_FUNC
        {
            public string KeyWord;
            public DoEvent Work;
        }


        List<EVENT_FUNC> EVENT_ALL = new List<EVENT_FUNC>()
        {
            new EVENT_FUNC(){KeyWord = "image", Work = new DoEvent(EVENT_ImageMsg) },
            new EVENT_FUNC(){KeyWord = "audio", Work = new DoEvent(EVENT_AudioMsg) },
            new EVENT_FUNC(){KeyWord = "sticker", Work = new DoEvent(EVENT_StickerMsg) },
            new EVENT_FUNC(){KeyWord = "template", Work = new DoEvent(EVENT_TemplateMsg) },
        };

        private readonly IConfiguration _config;
        public LineBotController(IConfiguration config)
        {
            _config = config;
        }


        [HttpPost]
        public IActionResult POST()
        {

            //get configuration from appsettings.json
            var token = _config.GetSection("LINE-Bot-Setting:channelAccessToken");
            var AdminUserId = _config.GetSection("LINE-Bot-Setting:adminUserID");
            var body = ""; //for JSON Body
            //create vot instance
            var bot = new Bot(token.Value);
            isRock.LineBot.MessageBase responseMsg = null;
            //message collection for response multi-message 
            List<isRock.LineBot.MessageBase> responseMsgs = new List<isRock.LineBot.MessageBase>();
            string JsonMsg = "";
            //LineUserInfo UserInfo = null;
            string ExceptionCmd = "";
            string ExceptionEvent = "";
            try
            {
                //get JSON Body
                using (StreamReader reader = new StreamReader(Request.Body, Encoding.UTF8))
                {
                    body = reader.ReadToEndAsync().Result;
                }
                //parsing JSON
                var ReceivedMessage = Utility.Parsing(body);

                //Get LINE Event
                var LineEvent = ReceivedMessage.events.FirstOrDefault();

                ExceptionEvent = LineEvent.type.ToLower();

                //prepare reply message
                if (LineEvent.type.ToLower() == "message") //訊息
                {
                    var LineContentID = ReceivedMessage.events.FirstOrDefault().message.id.ToString();

                    switch (LineEvent.message.type.ToLower())
                    {
                        case "text": //文字
                            ExceptionCmd = LineEvent.message.text;
                            JsonMsg = DoWork(LineEvent.message.text, ref responseMsgs);
                            break;

                        case "sticker": //表情
                            DoWork_Sticker(LineEvent, ref responseMsgs);
                            break;

                        case "image": //圖片
                            DoWork_Image(LineEvent, LineContentID, ref responseMsgs);
                            break;

                        case "audio": //聲音
                            DoWork_Audio(LineEvent, LineContentID, ref responseMsgs);
                            break;

                        case "video": //影片
                            DoWork_Video(LineEvent, LineContentID, ref responseMsgs);
                            break;

                        case "location": //位置資訊
                            DoWork_Location(LineEvent, ref responseMsgs);
                            break;

                        case "file": //檔案
                            DoWork_File(LineEvent, LineContentID, ref responseMsgs);
                            break;

                        default:
                            responseMsg = new TextMessage($"None handled message type : { LineEvent.message.type}");
                            responseMsgs.Add(responseMsg);
                            break;
                    }
                }
                else if (LineEvent.type.ToLower() == "leave") // 離開群組
                {

                }
                else if (LineEvent.type.ToLower() == "join") // 加入群組
                {

                }
                else if (LineEvent.type.ToLower() == "postback") //
                {

                }
                else if (LineEvent.type.ToLower() == "follow") // 解鎖好友/加入好友
                {

                }
                else if (LineEvent.type.ToLower() == "unfollow") // 封鎖好友/刪除好友
                {

                }
                else if (LineEvent.type.ToLower() == "memberjoined") //成員加入
                {

                }
                else if (LineEvent.type.ToLower() == "memberleft") //成員離開
                {

                }
                else if (LineEvent.type.ToLower() == "unsend") // 取消發送消息
                {

                }
                else //未知Event
                {
                    responseMsg = new TextMessage($"None handled event type : 【{ LineEvent.type}】");
                    responseMsgs.Add(responseMsg);
                }

                //回覆訊息
                if (responseMsgs.Count > 0)
                {
                    bot.ReplyMessage(LineEvent.replyToken, responseMsgs);
                }

                //Flax Message用
                if (JsonMsg != "")
                {
                    bot.ReplyMessageWithJSON(LineEvent.replyToken, JsonMsg);
                }

                return Ok();
            }
            catch (Exception ex)
            {
                //如果有錯誤，push給admin
                bot.PushMessage(AdminUserId.Value, $"Exception : 【{ExceptionEvent}】【{ExceptionCmd}】\n{ex.Message}");
                return Ok();
            }
        }

        /// <summary>
        /// 文字訊息處理
        /// </summary>
        /// <param name="Message"></param>
        /// <param name="response"></param>
        private string DoWork(string Message, ref List<isRock.LineBot.MessageBase> response)
        {
            string[] CMD = Message.Split(' ');
            string Msg = "";
            for (int i = 0; i < EVENT_ALL.Count; i++)
            {
                if (CMD[0].ToLower().Equals(EVENT_ALL[i].KeyWord))
                {
                    Msg = EVENT_ALL[i].Work(Message.Substring(EVENT_ALL[i].KeyWord.Length).Trim(), ref response);
                    return Msg;
                }
            }
            response.Add(new isRock.LineBot.TextMessage($"you said : {Message}"));
            return "";
        }

        private string DoWork_Postback(string Message, ref List<isRock.LineBot.MessageBase> response)
        {
            return "";
        }

        private static void DoWork_Sticker(isRock.LineBot.Event LineEvent, ref List<isRock.LineBot.MessageBase> response)
        {
            //response.Add(new isRock.LineBot.TextMessage("收到了表情"));
        }

        private void DoWork_Audio(isRock.LineBot.Event LineEvent, string LineID, ref List<isRock.LineBot.MessageBase> response)
        {
            //response.Add(new isRock.LineBot.TextMessage("收到了聲音"));
        }

        private void DoWork_Image(isRock.LineBot.Event LineEvent, string LineID, ref List<isRock.LineBot.MessageBase> response)
        {
            //response.Add(new isRock.LineBot.TextMessage("收到了圖片"));
        }

        private void DoWork_Video(isRock.LineBot.Event LineEvent, string LineID, ref List<isRock.LineBot.MessageBase> response)
        {
            //response.Add(new isRock.LineBot.TextMessage("收到了影片"));
        }

        private static void DoWork_Location(isRock.LineBot.Event LineEvent, ref List<isRock.LineBot.MessageBase> response)
        {
            //response.Add(new isRock.LineBot.TextMessage("收到了位置訊息"));
        }

        private void DoWork_File(isRock.LineBot.Event LineEvent, string LineID, ref List<isRock.LineBot.MessageBase> response)
        {
            //response.Add(new isRock.LineBot.TextMessage("收到了檔案"));
        }
        
        private static string EVENT_ImageMsg(string Message, ref List<MessageBase> response)
        {
            string sURL;

            switch(Message)
            {
                case "1":
                    sURL = "https://upload.cc/i1/2020/09/15/XYZDz9.jpg";
                    break;
                case "2":
                    sURL = "https://upload.cc/i1/2020/09/15/bONUCn.jpg";
                    break;
                default:
                    sURL = "https://upload.cc/i1/2020/09/15/ZDwYTk.jpg";
                    break;
            }
            var Msg = new ImageMessage(new Uri(sURL), new Uri(sURL));

            response.Add(Msg);
            return "";
        }

        private static string EVENT_AudioMsg(string Message, ref List<MessageBase> response)
        {
            string sURL = "https://203146b5091e8f0aafda-15d41c68795720c6e932125f5ace0c70.ssl.cf1.rackcdn.com/530000063.ogg";
            var Msg = new AudioMessage(new Uri(sURL), 17000);

            response.Add(Msg);
            return "";
        }

        private static string EVENT_StickerMsg(string Message, ref List<MessageBase> response)
        {
            response.Add(new StickerMessage(1, 2));
            return "";
        }

        private static string EVENT_TemplateMsg(string Message, ref List<MessageBase> response)
        {
            //define actions
            var act1 = new MessageAction()
            { text = "action1", label = "test action1" };
            var act2 = new MessageAction()
            { text = "action2", label = "test action2" };

            var tmp = new ButtonsTemplate()
            {
                text = "Button Template text",
                title = "Button Template title",
                thumbnailImageUrl = new Uri("https://upload.cc/i1/2020/09/15/WydcUk.jpg"),
            };

            tmp.actions.Add(act1);
            tmp.actions.Add(act2);
            //add TemplateMessage into responseMsgs
            response.Add(new TemplateMessage(tmp));
            return "";
        }
        
    }
}