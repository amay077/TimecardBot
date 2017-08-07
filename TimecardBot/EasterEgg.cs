using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using Microsoft.Bot.Connector;
using Microsoft.Bot.Builder.Dialogs;
using System.Threading.Tasks;
using System.Diagnostics;

namespace TimecardBot
{
    public sealed class EasterEgg
    {
        public async Task PostGanbaruzoi(IDialogContext context)
        {
            try
            {
                var thisExe = System.Reflection.Assembly.GetExecutingAssembly();
                using (var file =
                    thisExe.GetManifestResourceStream("TimecardBot.Images.ganbaruzoi.png"))
                {
                    var imageArray = new byte[file.Length];
                    file.Read(imageArray, 0, (int)file.Length);

                    var mes = context.MakeMessage();
                    //mes.Text = "がんばるぞい！";
                    mes.Locale = "ja";
                    var imageData = Convert.ToBase64String(imageArray);
                    var attachment = new Attachment
                    {
                        Name = "ganbaruzoi.png",
                        ContentType = "image/png",
                        ContentUrl = $"data:image/png;base64,{imageData}"
                    };
                    mes.Attachments.Add(attachment);

                    //mes.Attachments.Add(
                    //    new HeroCard
                    //    {
                    //        Title = $"ライオン",
                    //        Images = new List<CardImage>
                    //        {
                    //        new CardImage
                    //        {
                    //            Url = "http://free-photos-ls04.gatag.net/thum01/gf01a201503290600.jpg"
                    //        }
                    //        },
                    //    }.ToAttachment());
                    await context.PostAsync(mes);
                }
            }
            catch (Exception ex)
            {
                Trace.WriteLine("PostGanbaruzoi failed. - " + ex.Message);
            }
        }
    }
}