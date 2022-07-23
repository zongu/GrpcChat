
namespace GrpcChat.Domain.Tests.Model
{
    using Google.Protobuf;
    using GrpcChat.Service;
    using Microsoft.VisualStudio.TestTools.UnitTesting;
    using NetCoreGrpc.Action;
    using NetCoreGrpc.Model;

    [TestClass]
    public class ActionModelTest
    {
        [TestMethod]
        public void ActionModel傳輸轉換測試()
        {
            IMessage msg = new ChatMessageAction()
            {
                ChatMessage = new ChatMessage()
                {
                    NickName = "TEST001",
                    Message = "Message",
                    CreateDateTimeStamp = 123456
                }
            };

            var action = new ActionModel()
            {
                Action = msg.GetType().Name,
                SerialNumber = 1,
                Content = msg.ToByteString()
            };

            Assert.AreEqual(action.Action, "ChatMessageAction");

            var outputResult = ChatMessageAction.Parser.ParseFrom(action.Content);

            Assert.AreEqual(outputResult.ChatMessage.NickName, "TEST001");
            Assert.AreEqual(outputResult.ChatMessage.Message, "Message");
            Assert.AreEqual(outputResult.ChatMessage.CreateDateTimeStamp, 123456);
        }
    }
}
