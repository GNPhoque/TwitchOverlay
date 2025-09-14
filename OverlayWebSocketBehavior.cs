using Newtonsoft.Json;
using System;
using System.Diagnostics;
using System.Windows;
using TwitchOverlay;
using WebSocketSharp;
using WebSocketSharp.Server;

public class OverlayWebSocketBehavior : WebSocketBehavior
{
	protected override void OnMessage(MessageEventArgs e)
	{
		Debug.WriteLine("Local WS received");
		Debug.WriteLine($"Local WS received: {e.Data}");

		try
		{
			var json = JsonConvert.DeserializeObject<dynamic>(e.Data);
			string command = json.command;

			Application.Current.Dispatcher.BeginInvoke(new Action(() =>
			{
				//PAINT
				if(command == IniParser.paintOne)
				{
					((MainWindow)Application.Current.MainWindow).DrawSplash();
				}
				else if (command == IniParser.paintTen)
				{
					for (int i = 0; i < 10; i++)
					{
						((MainWindow)Application.Current.MainWindow).DrawSplash();
					}
				}
				else if (command == IniParser.paintHundread)
				{
					for (int i = 0; i < 100; i++)
					{
						((MainWindow)Application.Current.MainWindow).DrawSplash();
					}
				}
				else if (command == IniParser.paintRemoveOne)
				{
					((MainWindow)Application.Current.MainWindow).RemoveSplash();
				}
				else if (command == IniParser.paintRemoveAll)
				{
					((MainWindow)Application.Current.MainWindow).RemoveAllSplash();
				}
				else if (command == IniParser.paintFillScreenProgressive)
				{
					((MainWindow)Application.Current.MainWindow).FillScreen();
				}
				else if (command == IniParser.paintEmptyScreenProgressive)
				{
					((MainWindow)Application.Current.MainWindow).ClearScreen();
				}

				//RAID
				else if (command == IniParser.raid)
				{
					for (int i = 0; i < (int)json.value; i++)
					{
						((MainWindow)Application.Current.MainWindow).DrawSplash();
					}
				}

				//DEATH COUNTER
				else if (command == IniParser.showDeathCounter)
				{
					((MainWindow)Application.Current.MainWindow).ShowText();
				}
				else if (command == IniParser.hideDeathCounter)
				{
					((MainWindow)Application.Current.MainWindow).HideText();
				}
				else if (command == IniParser.updateDeathCounter)
				{
					string value = json.value;
					int number = -1;

					if (!string.IsNullOrEmpty(value) && int.TryParse(value, out number))
					{
						((MainWindow)Application.Current.MainWindow).UpdateText($"Morts : {value}");
						if (((MainWindow)Application.Current.MainWindow).IsDeathCounterVisible())
						{
							if (number % 100 == 0)
							{
								for (int i = 0; i < 100; i++)
								{
									((MainWindow)Application.Current.MainWindow).DrawSplash();
								}
							}
							else if (number % 10 == 0)
							{
								for (int i = 0; i < 10; i++)
								{
									((MainWindow)Application.Current.MainWindow).DrawSplash();
								}
							}
							else
							{
								((MainWindow)Application.Current.MainWindow).DrawSplash();
							}
						}
					}
				}

				//EXIT
				else if (command == IniParser.exit)
				{
					((MainWindow)Application.Current.MainWindow).Shutdown();
				}
			}));
		}
		catch (Exception ex)
		{
			Debug.WriteLine($"Error parsing local WS: {ex.Message}");
		}
	}

	protected override void OnOpen()
	{
		Debug.WriteLine("[WS OPEN] Client connected");
		base.OnOpen();
	}

	protected override void OnClose(CloseEventArgs e)
	{
		Debug.WriteLine($"[WS CLOSE] Code={e.Code}, Reason={e.Reason}");
		base.OnClose(e);
	}

	protected override void OnError(ErrorEventArgs e)
	{
		Debug.WriteLine($"[WS ERROR] {e.Message}\n{e.Exception}");
		base.OnError(e);
	}
}