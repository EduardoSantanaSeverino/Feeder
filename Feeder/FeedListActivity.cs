using System;
using Android.App;
using Android.Content;
using Android.Runtime;
using Android.Views;
using Android.Widget;
using Android.OS;
using System.Collections.Generic;
using System.IO;
using System.Runtime.Serialization.Formatters.Binary;
using System.Net.Http;
using System.Xml.Linq;
using System.Linq;
using Newtonsoft.Json;

namespace Feeder
{
	[Activity(Label = "Feeder", MainLauncher = true, Icon = "@drawable/icon")]
	public class FeedListActivity : Activity
	{
		private List<RssFeed> _feeds;
		private ListView _feedListView;
		private Button _addFeedButton;
		private const string FEED_FILE_NAME = "FeedData.bin";
		private string _filePath;

		public FeedListActivity()
		{
			_feeds = new List<RssFeed>();
			var path = System.Environment.GetFolderPath(System.Environment.SpecialFolder.Personal);
			path = System.IO.Path.Combine(path, FEED_FILE_NAME);
		}
		protected override void OnCreate(Bundle bundle)
		{
			base.OnCreate(bundle);

			//// Set our view from the "FeedList" layout resource
			SetContentView(Resource.Layout.FeedList);

			_feedListView = FindViewById<ListView>(Resource.Id.feedList);
			_addFeedButton = FindViewById<Button>(Resource.Id.addFeedButton);

			_feedListView.ItemClick += _feedListView_ItemClick;
			_addFeedButton.Click += _addFeedButton_Click;
			
			if (File.Exists(_filePath))
			{
				using (var fs = new FileStream(_filePath, FileMode.OpenOrCreate, FileAccess.ReadWrite))
				{
					var formatter = new BinaryFormatter();
					try
					{
						_feeds = (List<RssFeed>)formatter.Deserialize(fs);
					}
					catch (Exception ex)
					{
						Android.Util.Log.Error("Feeder", "Se ha encontrado un error en feeder: {0}", ex.Message);
					}
					if (_feeds.Count > 0)
					{
						UpdateList();
					}
				}
			}
		}

		private void UpdateList()
		{
			_feedListView.Adapter = new FeedListAdapter(_feeds.ToArray(), this);
		}

		private void _addFeedButton_Click(object sender, EventArgs e)
		{
			var intent = new Intent(this, typeof(AddFeedActivity));
			StartActivityForResult(intent, 0);
		}

		protected override void OnActivityResult(int requestCode, [GeneratedEnum] Result resultCode, Intent data)
		{
			base.OnActivityResult(requestCode, resultCode, data);	
			if(resultCode == Result.Ok)
			{
				var url = data.GetStringExtra("url");
				AddFeedUrl(url);
			}
		}

		private async void AddFeedUrl(string url)
		{
			var newFeed = new RssFeed
			{
				DateAdded = DateTime.Now,
				URL = url
			};
			using (var client = new HttpClient())
			{
				var xmlFeed = await client.GetStringAsync(url);
				var doc = new XDocument(xmlFeed);

				var channel = doc.Descendants("channel").FirstOrDefault().Element("title").Value;
				newFeed.Name = channel;

				XNamespace dc = "http://purl.org/dc/elements/1.1/";
				newFeed.Items = (from item in doc.Descendants("item")
								 select new RssItem
								 {
									 Titile = item.Element("title").Value,
									 PubDate = item.Element("pubDate").Value,
									 Creator = item.Element(dc + "creator").Value,
									 Link = item.Element("link").Value
								 }).ToList();

				_feeds.Add(newFeed);
			}
		}

		private void _feedListView_ItemClick(object sender, AdapterView.ItemClickEventArgs e)
		{
			var intent = new Intent(this, typeof(FeedItemListActivity));
			var selectedFeed = _feeds[e.Position];
			var feed = JsonConvert.SerializeObject(selectedFeed);
			intent.PutExtra("feed", feed);
		}
	}
}

