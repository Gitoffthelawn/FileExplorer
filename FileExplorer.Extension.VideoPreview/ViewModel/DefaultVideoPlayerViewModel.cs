using System;
using System.Threading.Tasks;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Animation;
using DevExpress.Mvvm;
using DevExpress.Mvvm.DataAnnotations;

namespace FileExplorer.Extension.VideoPreview.ViewModel
{
    public class DefaultVideoPlayerViewModel : VideoPlayerViewModel
    {
        [ServiceProperty(Key = "DefaultVideoPlayerService")]
        public virtual IUIObjectService VideoPlayerService { get { return null; } }

        public MediaClock MediaClock { get; private set; }

        public dynamic MediaPlayer => VideoPlayerService.Object;

        public override int PlaybackPosition => Convert.ToInt32(Position);

        public override Task PreviewFile(string filePath)
        {
            MediaPlayer.ScrubbingEnabled = false;
            
            if (MediaClock != null)
            {
                MediaClock.Completed -= OnCompleted;
                MediaClock.CurrentTimeInvalidated -= OnCurrentTimeInvalidated;
                MediaClock.CurrentGlobalSpeedInvalidated -= OnCurrentGlobalSpeedInvalidated;
            }

            MediaTimeline mediaTimeline = new MediaTimeline(new Uri(filePath));
            MediaClock = mediaTimeline.CreateClock();

            switch (VideoPreviewSettings.Default.LoadBehavior)
            {
                case LoadBehavior.None:
                    MediaPlayer.LoadedBehavior = MediaState.Close;
                    MediaClock.Controller.Pause();
                    break;

                case LoadBehavior.Pause:
                    MediaPlayer.LoadedBehavior = MediaState.Pause;
                    MediaPlayer.Clock = MediaClock;
                    MediaClock.Controller.Pause();
                    break;

                case LoadBehavior.Play:
                    MediaPlayer.LoadedBehavior = MediaState.Play;
                    MediaPlayer.Clock = MediaClock;
                    MediaClock.Controller.Begin();
                    break;
            }

            MediaClock.Completed += OnCompleted;
            MediaClock.CurrentTimeInvalidated += OnCurrentTimeInvalidated;
            MediaClock.CurrentGlobalSpeedInvalidated += OnCurrentGlobalSpeedInvalidated;

            return base.PreviewFile(filePath);
        }

        public override void UnloadFile()
        {
            base.UnloadFile();
            MediaClock.Controller.Remove();
        }

        public override void Play()
        {
            MediaPlayer.Clock = MediaClock;
            MediaClock?.Controller.Resume();  
        }

        public override void Pause()
        {
            MediaClock?.Controller.Pause();
        }

        public override void Stop()
        {
            MediaClock?.Controller.Begin();
            MediaClock?.Controller.Pause();
        }

        public override void Next()
        {
            int jump = VideoPreviewSettings.Default.NextTimeJump;

            if (Duration - Position > jump)
                MediaClock?.Controller.Seek(TimeSpan.FromSeconds(Position + jump), TimeSeekOrigin.BeginTime);
            else
                MediaClock?.Controller.SkipToFill();
        }

        public override void Previous()
        {
            int jump = VideoPreviewSettings.Default.PreviousTimeJump;

            if (Position > jump)
                MediaClock?.Controller.Seek(TimeSpan.FromSeconds(Position - jump), TimeSeekOrigin.BeginTime);
            else
                MediaClock?.Controller.Begin();
        }

        public override void Seek(int seconds)
        {
            MediaPlayer.Clock = MediaClock;
            MediaClock.Controller?.Seek(TimeSpan.FromSeconds(seconds), TimeSeekOrigin.BeginTime);
        }

        public override void MediaOpened()
        {
            base.MediaOpened();

            PlayCommand.RaiseCanExecuteChanged();
            MediaPlayer.ScrubbingEnabled = true;
            Duration = MediaPlayer.NaturalDuration.TimeSpan.TotalSeconds;
        }

        protected void OnPositionChanged()
        {
            if (MediaClock?.IsPaused == true)
                MediaClock.Controller?.Seek(TimeSpan.FromSeconds(Position), TimeSeekOrigin.BeginTime);
        }

        private void OnCompleted(object sender, EventArgs e)
        {
            Stop();
        }

        private void OnCurrentGlobalSpeedInvalidated(object sender, EventArgs e)
        {
            IsPlaying = MediaClock?.IsPaused == false;
        }

        private void OnCurrentTimeInvalidated(object sender, EventArgs e)
        {
            if (MediaClock?.CurrentTime.HasValue == true)
            {
                double position = Math.Truncate(MediaClock.CurrentTime.Value.TotalSeconds);
                if (Position != position)
                    Position = position;
            }
        }
    }
}
