using System;
using System.Threading.Tasks;
using System.Windows.Media;
using DevExpress.Mvvm;
using FileExplorer.Common;

namespace FileExplorer.Extension.VideoPreview.ViewModel
{
    public abstract class VideoPlayerViewModel
    {
        public abstract void Play();

        public abstract void Pause();

        public abstract void Stop();

        public abstract void Next();

        public abstract void Previous();

        public abstract void Seek(int seconds);

        public abstract int PlaybackPosition { get; }

        public virtual bool Opened { get; set; }

        public virtual bool IsMuted { get; set; }

        public virtual bool IsPlaying { get; set; }

        public virtual double Position { get; set; }

        public virtual double Duration { get; set; }

        public virtual double Volume { get; set; } = 0.5;

        public virtual string ErrorMessage { get; set; }

        public virtual ImageSource ThumbnailImage {  get; set; }

        public virtual IDialogService DialogService { get { return null; } }

        public DelegateCommand PlayCommand => new DelegateCommand(Play, () => Opened);

        public DelegateCommand PauseCommand => new DelegateCommand(Pause, () => IsPlaying);

        public DelegateCommand StopCommand => new DelegateCommand(Stop, () => Opened);

        public DelegateCommand NextCommand => new DelegateCommand(Next, () => Opened);

        public DelegateCommand PreviousCommand => new DelegateCommand(Previous, () => Opened);

        public static PersistentDictionary<string, int> History { get; private set; }

        public virtual async Task PreviewFile(string filePath)
        {
            if (VideoPreviewSettings.Default.ShowThumbnails)
                await GenerateThumbnails(filePath);
        }

        public virtual void UnloadFile()
        {
            Stop();
        }

        public virtual async Task GenerateThumbnails(string filePath)
        {
            ThumbnailImage = await VideoThumbnailHelper.GenerateThumbnails(new Uri(filePath), 
                VideoPreviewSettings.Default.ThumbnailRows, VideoPreviewSettings.Default.ThumbnailColumns, VideoPreviewSettings.Default.TimestampPosition);
        }

        public virtual void TogglePlayPause()
        {
            if (IsPlaying)
                Pause();
            else
                Play();
        }

        public virtual void MediaOpened()
        {
            Opened = true;
            ErrorMessage = String.Empty;
        }

        public virtual void ShowSettings()
        {
            Pause();

            var settings = VideoPreviewSettings.Load();
            if (DialogService.ShowDialog(MessageButton.OKCancel, Properties.Resources.Settings, settings) == MessageResult.OK)
            {
                var oldSettings = VideoPreviewSettings.Load();

                VideoPreviewSettings.Default.SetValues(settings);
                VideoPreviewSettings.Save();

                Messenger.Default.Send(new SettingsChangedMessage(oldSettings));
            }
        }

        static VideoPlayerViewModel()
        {
            History = new PersistentDictionary<string, int>("VideoPreviewHistory");
        }
    }
}
