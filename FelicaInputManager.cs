using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Reactive.Subjects;

namespace DenxTouchClient
{
    static class FelicaInputManager
    {
        public static Subject<string> felicaInfoStream = new Subject<string>();//ICカードのIDが流れてくる
        const string doshishaPMm = "0120220427674EFF";//学生証のPMm

        //CardIDの取得
        //IDmが流れて来たらfelicaInfoStreamに流す
        public static void pool()
        {
            try
            {
                using (FelicaLib.Felica f = new FelicaLib.Felica())
                {

                    f.Polling(0xFFFF);
                    byte[] data = f.IDm();
                    byte[] pdata = f.PMm();

                    String dataStr = "";
                    for (int i = 0; i < data.Length; i++)
                    {
                        dataStr += data[i].ToString("X2");
                    }

                    String pdataStr = "";
                    for (int i = 0; i < pdata.Length; i++)
                    {
                        pdataStr += pdata[i].ToString("X2");
                    }

                    //Console.WriteLine(pdataStr);

                    //pMmが学生証のものかどうかをチェック
                    if (pdataStr == doshishaPMm)
                    {
                        felicaInfoStream.OnNext(dataStr);
                    }
                    else
                    {
                        felicaInfoStream.OnNext(null);
                        return;
                    }
                }
            }
            catch (FelicaNotLoadedExeption e)
            {
                felicaInfoStream.OnNext(null);
            }
            catch (FelicaNotConnectedExeption e)
            {
                felicaInfoStream.OnError(e);
            }
            catch (Exception e)
            {
                felicaInfoStream.OnError(e);
            }
        }
    }
}
