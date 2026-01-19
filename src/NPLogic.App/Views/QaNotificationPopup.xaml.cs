using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Windows;
using Microsoft.Extensions.DependencyInjection;
using NPLogic.Core.Models;
using NPLogic.Data.Repositories;
using NPLogic.Data.Services;

namespace NPLogic.Views
{
    /// <summary>
    /// QaNotificationPopup.xaml에 대한 상호 작용 논리
    /// QA 답변 알림 팝업
    /// 피드백 섹션 18: 알림 기능
    /// - 답변이 오면 배정된 계정 로그인 시 팝업 알람
    /// - 닫기를 누르지 않으면 꺼지지 않고 떠있어야 함
    /// </summary>
    public partial class QaNotificationPopup : Window
    {
        private readonly QaNotificationRepository? _notificationRepository;
        private readonly Guid _userId;
        private List<QaNotification> _notifications = new();

        /// <summary>
        /// 알림 목록으로 초기화
        /// </summary>
        public QaNotificationPopup(List<QaNotification> notifications, Guid userId, QaNotificationRepository? repository = null)
        {
            InitializeComponent();
            
            _notifications = notifications;
            _userId = userId;
            _notificationRepository = repository;
            
            LoadNotifications();
        }

        /// <summary>
        /// 알림 목록 로드
        /// </summary>
        private void LoadNotifications()
        {
            if (_notifications.Count == 0)
            {
                NotificationList.Visibility = Visibility.Collapsed;
                EmptyMessage.Visibility = Visibility.Visible;
                NotificationCountText.Text = "새로운 알림이 없습니다";
            }
            else
            {
                NotificationList.ItemsSource = _notifications;
                NotificationList.Visibility = Visibility.Visible;
                EmptyMessage.Visibility = Visibility.Collapsed;
                NotificationCountText.Text = $"{_notifications.Count}건의 새로운 답변이 있습니다";
            }
        }

        /// <summary>
        /// 확인 버튼 클릭
        /// </summary>
        private async void CloseButton_Click(object sender, RoutedEventArgs e)
        {
            // 모두 읽음으로 표시 체크 시 처리
            if (MarkAllReadCheckBox.IsChecked == true && _notificationRepository != null)
            {
                try
                {
                    await _notificationRepository.MarkAllAsReadAsync(_userId);
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"알림 읽음 처리 실패: {ex.Message}");
                }
            }
            
            DialogResult = true;
            Close();
        }

        /// <summary>
        /// 미읽은 알림이 있는지 확인하고 팝업 표시
        /// </summary>
        public static async Task<bool> ShowIfHasNotificationsAsync(Window owner, Guid userId)
        {
            try
            {
                var serviceProvider = App.ServiceProvider;
                if (serviceProvider == null) return false;

                var supabaseService = serviceProvider.GetRequiredService<SupabaseService>();
                var repository = new QaNotificationRepository(supabaseService);
                
                var notifications = await repository.GetUnreadByUserIdAsync(userId);
                
                if (notifications.Count > 0)
                {
                    var popup = new QaNotificationPopup(notifications, userId, repository)
                    {
                        Owner = owner
                    };
                    popup.ShowDialog();
                    return true;
                }
                
                return false;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"알림 조회 실패: {ex.Message}");
                return false;
            }
        }
    }
}
