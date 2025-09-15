using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Runtime.InteropServices;
using System.Windows.Input;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using WebSocketSharp;
using WebSocket = WebSocketSharp.WebSocket;
using System.Diagnostics;
using Newtonsoft.Json;
using System.Net.Http;
using System.Text;
using WebSocketSharp.Server;
using System.Windows.Threading;
using WpfAnimatedGif;
using System.IO;
using TwitchOverlay.Properties;

namespace TwitchOverlay
{
	/// <summary>
	/// Logique d'interaction pour MainWindow.xaml
	/// </summary>
	public partial class MainWindow : Window
	{
		public class DisplayImageReference
		{
			public string path;
			public int width;
			public int height;
			public int duration;
			public int weight;
			public AnimatableBitmapImage image;

			public DisplayImageReference(string p, int x, int y, int dur, int w)
			{
				path = p;
				width = x;
				height = y;
				duration = dur;
				weight = w;
			}
		}

		public class AnimatableBitmapImage
		{
			public BitmapImage bitmapImage;
			public bool isAnimatable;

			public AnimatableBitmapImage(BitmapImage image, bool animatable = false)
			{
				bitmapImage = image;
				isAnimatable = animatable;
			}
		}

		// Import des fonctions Win32
		[DllImport("user32.dll")] private static extern int SetWindowLong(IntPtr hWnd, int nIndex, int dwNewLong);
		[DllImport("user32.dll")] private static extern int GetWindowLong(IntPtr hWnd, int nIndex);

		const int GWL_EXSTYLE = -20;
		const int WS_EX_LAYERED = 0x80000;
		const int WS_EX_TRANSPARENT = 0x20;
		const int WS_EX_TOPMOST = 0x8;
		const int DELAY_BEFORE_DELETE = 600000;

		private List<DisplayImageReference> imageNames = new List<DisplayImageReference>()
		{
			new DisplayImageReference( "white.png", 120,128, 600, 100),
			new DisplayImageReference( "yellow.png", 120,128, 600, 100),
			new DisplayImageReference( "red.png", 120,128, 600, 100),
			new DisplayImageReference( "green.png", 120,128, 600, 100),
			new DisplayImageReference( "orange.png", 120,128, 600, 100),
			new DisplayImageReference( "purple.png", 120,128, 600, 100),
			new DisplayImageReference( "teal.png", 120,128, 600, 100),

			new DisplayImageReference( "1tache56.png", 120,128, 600, 100),
			new DisplayImageReference( "10taches56.png", 120,128, 600, 100),

			new DisplayImageReference( "Bug.gif", 120,128, 600, 100),
			new DisplayImageReference( "cestphoque headbang.gif", 120, 128, 600, 100),
			new DisplayImageReference( "phoqueTyping.gif", 120,128, 600, 100),

			new DisplayImageReference( "phoqueTyping.gif", 512, 512, 120, 10),
			new DisplayImageReference( "phoqueTyping.gif", 1024, 1024, 10, 1),
		};

		private bool isFollowingHand;
		private Image handPositionImage;
		private DisplayImageReference raclette = new DisplayImageReference("raclette.png", 375, 166, 600, 100);
		private (float x, float y) raclettePosition = (0f, 0f);

		private Random random = new Random();
		private List<Image> images = new List<Image>();
		private bool eventsSubcribed;

		public MainWindow()
		{
			InitializeComponent();

			FetchSettings();
			FetchImages();

			SetupTwitchWebSocket();
			SetupLocalWebSocket();

			SetupWindow();
			RefreshTopMostPeriodically();
		}

		private void FetchSettings()
		{
			IniParser.ReadConfig();
		}

		private void FetchImages()
		{
			BitmapImage bmp;

			foreach (var item in imageNames)
			{
				// Vérifie si c'est un gif
				if (item.path.EndsWith(".gif", StringComparison.OrdinalIgnoreCase))
				{
					bmp = new BitmapImage();
					using (var fs = new FileStream(item.path, FileMode.Open, FileAccess.Read, FileShare.Read))
					{
						var ms = new MemoryStream();
						fs.CopyTo(ms);
						ms.Position = 0;

						bmp.BeginInit();
						bmp.CacheOption = BitmapCacheOption.OnLoad;
						bmp.StreamSource = ms;
						bmp.EndInit();
					}
					item.image = new AnimatableBitmapImage(bmp, true);
				}
				else
				{
					bmp = new BitmapImage(new Uri(item.path, UriKind.Relative));
					item.image = new AnimatableBitmapImage(bmp);
				}
			}

			bmp = new BitmapImage(new Uri(raclette.path, UriKind.Relative));
			raclette.image = new AnimatableBitmapImage(bmp);
		}

		private void SetupTwitchWebSocket()
		{
			WebSocket ws = new WebSocket(IniParser.connectionAdress);
			//WebSocket ws = new WebSocket("ws://127.0.0.1:8080/ws"); // local tests with twitch CLI
			ws.OnOpen += Ws_OnOpen;
			ws.OnMessage += Ws_OnMessage;
			ws.Connect();
		}

		//wscat -c ws:localhost:4049/control and send {"command":"splash"} {"command":"multi"} {"command":"clear"}
		private static void SetupLocalWebSocket()
		{
			var wssv = new WebSocketServer($"{IniParser.ip}:{IniParser.port}");
			wssv.AddWebSocketService<OverlayWebSocketBehavior>($"/{IniParser.behaviorName}");
			wssv.Start();
			Console.WriteLine($"Local WebSocket server started at {IniParser.ip}:{IniParser.port}/{IniParser.behaviorName}");
		}

		private void SetupWindow()
		{
			//Style
			WindowStyle = WindowStyle.None;
			WindowState = WindowState.Maximized;
			ResizeMode = ResizeMode.NoResize;
			AllowsTransparency = true;
			Background = Brushes.Transparent;
			Topmost = true;

			//Position
			Top = 0;
			Left = 0;
			Width = SystemParameters.PrimaryScreenWidth;
			Height = SystemParameters.PrimaryScreenHeight;

			Loaded += (s, e) =>
			{
				var hwnd = new System.Windows.Interop.WindowInteropHelper(this).Handle;
				int style = GetWindowLong(hwnd, GWL_EXSTYLE);
				SetWindowLong(hwnd, GWL_EXSTYLE, style | WS_EX_LAYERED | WS_EX_TRANSPARENT | WS_EX_TOPMOST);
			};

			KeyDown += Overlay_KeyDown;
		}

		private void Ws_OnOpen(object sender, EventArgs e)
		{
			Debug.WriteLine(e.ToString());
		}

		private async void Ws_OnMessage(object sender, MessageEventArgs e)
		{
			if (e.Data.StartsWith("{{"))
			{
				e.Data.Remove(0, 1);
				e.Data.Remove(e.Data.Length - 2, 1);
			}

			var json = JsonConvert.DeserializeObject<dynamic>(e.Data);

			if (json.metadata.message_type == "session_keepalive")
			{
				Console.WriteLine($"{DateTime.Now} Received Keep Alive");
				return;
			}
			else if (json.metadata.message_type == "session_reconnect")
			{
				string url = json.payload.session.reconnect_url;
				WebSocket ws = new WebSocket(url);
				ws.OnOpen += Ws_OnOpen;
				ws.OnMessage += Ws_OnMessage;
				ws.Connect();
			}
			else if (json.metadata.message_type == "session_welcome")
			{
				string sessionId = json.payload.session.id;
				if (!eventsSubcribed)
				{
					eventsSubcribed = true;
					await SubscribeChannelPointsAsync(sessionId);
					await SubscribeRaidsAsync(sessionId);
				}
				return;
			}
			else if (json.payload.@event.reward.title == IniParser.rewardPaintTen)
			{
				Console.WriteLine($"{DateTime.Now} Received 10 Tâches");
				Application.Current.Dispatcher.Invoke(() =>
				{
					for (int i = 0; i < 10; i++)
					{
						DrawSplash();
					}
				});
				return;
			}
			else if (json.payload.@event.reward.title == IniParser.rewardPaintOne)
			{
				Console.WriteLine($"{DateTime.Now} Received Une tâche");
				Application.Current.Dispatcher.Invoke(() =>
				{
					DrawSplash();
				});
				return;
			}
				
			Debug.WriteLine(e.Data);
			Console.WriteLine(json.payload);
		}

		private async Task SubscribeChannelPointsAsync(string sessionId)
		{
			var payload = new
			{
				type = "channel.channel_points_custom_reward_redemption.add",
				version = "1",
				condition = new
				{
					broadcaster_user_id = IniParser.broadcasterUserId
				},
				transport = new
				{
					method = "websocket",
					session_id = sessionId
				}
			};

			string json = JsonConvert.SerializeObject(payload);

			using (HttpClient client = new HttpClient())
			{
				HttpRequestMessage message = new HttpRequestMessage
				{
					Method = HttpMethod.Post,
					RequestUri = new Uri(IniParser.eventSubscriptionAdress),
					//RequestUri = new Uri("http://127.0.0.1:8080/eventsub/subscriptions"), // local tests with twitch CLI
					Headers =
						{
							{ "Authorization", $"Bearer {IniParser.accessToken}" },
							{ "Client-Id", IniParser.clientId }
						},
					Content = new StringContent(json, Encoding.UTF8, "application/json")
				};

				var response = await client.SendAsync(message);
				string responseBody = await response.Content.ReadAsStringAsync();

				Console.WriteLine($"SUBSCRIPTION : {response.StatusCode}");
				Console.WriteLine(responseBody);
			}
		}

		private async Task SubscribeRaidsAsync(string sessionId)
		{
			var payload = new
			{
				type = "channel.raid",
				version = "1",
				condition = new
				{
					to_broadcaster_user_id = IniParser.broadcasterUserId
				},
				transport = new
				{
					method = "websocket",
					session_id = sessionId
				}
			};

			string json = JsonConvert.SerializeObject(payload);

			using (HttpClient client = new HttpClient())
			{
				HttpRequestMessage message = new HttpRequestMessage
				{
					Method = HttpMethod.Post,
					RequestUri = new Uri(IniParser.eventSubscriptionAdress),
					//RequestUri = new Uri("http://127.0.0.1:8080/eventsub/subscriptions"), // local tests with twitch CLI
					Headers =
						{
							{ "Authorization", $"Bearer {IniParser.accessToken}" },
							{ "Client-Id", IniParser.clientId }
						},
					Content = new StringContent(json, Encoding.UTF8, "application/json")
				};

				var response = await client.SendAsync(message);
				string responseBody = await response.Content.ReadAsStringAsync();

				Console.WriteLine($"SUBSCRIPTION : {response.StatusCode}");
				Console.WriteLine(responseBody);
			}
		}

		private void Overlay_KeyDown(object sender, KeyEventArgs e)
		{
			if (e.Key == Key.Space)
			{
				DrawSplash();
			}
			else if (e.Key == Key.Escape)
			{
				RemoveSplash();
			}
			else if (e.Key == Key.F)
			{
				FillScreen();
			}			
			else if (e.Key == Key.C)
			{
				ClearScreen();
			}
		}

		public void DrawSplash(float left = -1, float top = -1)
		{
			DisplayImageReference bmp;
			bool isRaclette = left != -1 && top != -1;

			if (isRaclette)
			{
				bmp = raclette;
			}
			else
			{
				bmp = GetWeightedImage(); 
			}

			Image image = new Image();

			// Vérifie si c'est un gif
			if (bmp.image.isAnimatable)
			{
				ImageBehavior.SetAnimatedSource(image, bmp.image.bitmapImage);
				ImageBehavior.SetAutoStart(image, true);

				image.Width = bmp.image.bitmapImage.Width;
				image.Height = bmp.image.bitmapImage.Height;
			}
			else
			{
				image.Source = bmp.image.bitmapImage;
				image.Height = bmp.image.bitmapImage.Height;
				image.Width = bmp.image.bitmapImage.Width;
			}

			ScaleTransform scaleTransform = new ScaleTransform();
			scaleTransform.ScaleX = bmp.width / image.Width;
			scaleTransform.ScaleY = bmp.height / image.Height;
			TransformGroup transformGroup = new TransformGroup();
			if (!isRaclette)
			{
				transformGroup.Children.Add(new RotateTransform(random.Next(0, 360)));
			}
			transformGroup.Children.Add(scaleTransform);
			image.RenderTransform = transformGroup;

			if (isRaclette)
			{
				Canvas.SetLeft(image, (int)SystemParameters.PrimaryScreenWidth * left);
				Canvas.SetTop(image, (int)SystemParameters.PrimaryScreenHeight * top);
				raclettePosition = ((float)SystemParameters.PrimaryScreenWidth * left, (float)SystemParameters.PrimaryScreenHeight * top);

				RootCanvas.Children.Add(image);
				handPositionImage = image;
			}
			else
			{
				// Position aléatoire
				Canvas.SetLeft(image, random.Next(0 + (int)Math.Round(scaleTransform.ScaleX * image.Width / 2f), (int)SystemParameters.PrimaryScreenWidth - (int)Math.Round(scaleTransform.ScaleX * image.Width / 2f)));
				Canvas.SetTop(image, random.Next(0 + (int)Math.Round(scaleTransform.ScaleY * image.Height / 2f), (int)SystemParameters.PrimaryScreenHeight - (int)Math.Round(scaleTransform.ScaleY * image.Height / 2f)));

				RootCanvas.Children.Add(image);
				images.Add(image);

				RemoveSplashAfterDelay(bmp.duration);
			}
		}

		private DisplayImageReference GetWeightedImage()
		{
			int totalWeight = 0;

			foreach (var item in imageNames)
			{
				totalWeight += item.weight;
			}

			int rng = random.Next(0, totalWeight);
			int progress = 0;

			foreach (var item in imageNames)
			{
				progress += item.weight;
				if(progress > rng)
				{
					return item;
				}
			}

			return null;
		}

		private async void RemoveSplashAfterDelay(int delay)
		{
			await Task.Delay(delay * 1000);
			RemoveSplash();
		}

		public void RemoveSplash()
		{
			if (images.Count == 0)
			{
				return;
			}

			var img = images.First();

			// Si c’est un gif animé → stop l’animation
			if (ImageBehavior.GetAnimatedSource(img) != null)
			{
				ImageBehavior.SetAnimatedSource(img, null);
			}

			// Libère la source pour que le GC puisse collecter
			img.Source = null;

			// Supprime du canvas
			RootCanvas.Children.Remove(img);
			images.RemoveAt(0);
		}

		public void RemoveAllSplash()
		{
			while (images.Count > 0)
			{
				RemoveSplash();
			}
		}

		public async void FillScreen()
		{
			for (int i = 0; i < 100; i++)
			{
				DrawSplash();
				await Task.Delay(10);
			}
		}

		public async void ClearScreen()
		{
			while(images.Any())
			{
				RemoveSplash();
				RemoveSplash();
				RemoveSplash();
				await Task.Delay(1);
			}

			GC.Collect();
		}

		public void Shutdown()
		{
			Application.Current.Shutdown();
		}

		public bool IsDeathCounterVisible()
		{
			return DeathCounter.Visibility == Visibility.Visible;
		}

		public void ShowText()
		{
			Dispatcher.Invoke(() =>
			{
				DeathCounter.Visibility = Visibility.Visible;
			});
		}

		public void HideText()
		{
			Dispatcher.Invoke(() =>
			{
				DeathCounter.Visibility = Visibility.Hidden;
			});
		}

		public void UpdateText(string message)
		{
			Dispatcher.Invoke(() =>
			{
				DeathCounter.Text = message;
			});
		}

		private void RefreshTopMostPeriodically()
		{
			DispatcherTimer timer = new DispatcherTimer();
			timer.Interval = TimeSpan.FromSeconds(10);
			timer.Tick += (s, e) =>
			{
				this.Topmost = false;
				this.Topmost = true;
			};
			timer.Start();
		}

		public void FollowHand()
		{
			isFollowingHand = true;
			if (handPositionImage != null)
			{
				handPositionImage.Visibility = Visibility.Visible;
			}
		}

		public void UnfollowHand()
		{
			isFollowingHand = false;
			handPositionImage.Visibility = Visibility.Hidden;
		}

		public void ShowImageAtHandPosition(float left, float top)
		{
			if (!isFollowingHand)
			{
				return;
			}

			if(handPositionImage == null)
			{
				DrawSplash(left, top);
			}
			else
			{
				if(Math.Abs(raclettePosition.x - left) < .01f && Math.Abs(raclettePosition.y - top) < .01f)
				{
					return;
				}
				Canvas.SetLeft(handPositionImage, (int)SystemParameters.PrimaryScreenWidth * left);
				Canvas.SetTop(handPositionImage, (int)SystemParameters.PrimaryScreenHeight * top);
			}
		}
	}
}
