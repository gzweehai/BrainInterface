using System;
using System.IO;
using System.Reactive.Linq;

namespace BrainCommon
{
    public static class TextReaderExtensions
    {
        public static IObservable<string> ToLineObservable(this TextReader reader,string exitStr=null)
        {
            if (exitStr == null) exitStr= string.Empty;
            return Observable.Create<string>(async (observer, token) =>
            {
                try
                {
                    while (!token.IsCancellationRequested)
                    {
                        var line = await reader.ReadLineAsync();
                        if (line.Equals(exitStr))
                            break;

                        observer.OnNext(line);
                    }

                    observer.OnCompleted();
                }
                catch (Exception error)
                {
                    observer.OnError(error);
                }
            });
        }
    }}