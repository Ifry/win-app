﻿/*
 * Copyright (c) 2020 Proton Technologies AG
 *
 * This file is part of ProtonVPN.
 *
 * ProtonVPN is free software: you can redistribute it and/or modify
 * it under the terms of the GNU General Public License as published by
 * the Free Software Foundation, either version 3 of the License, or
 * (at your option) any later version.
 *
 * ProtonVPN is distributed in the hope that it will be useful,
 * but WITHOUT ANY WARRANTY; without even the implied warranty of
 * MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
 * GNU General Public License for more details.
 *
 * You should have received a copy of the GNU General Public License
 * along with ProtonVPN.  If not, see <https://www.gnu.org/licenses/>.
 */

using GalaSoft.MvvmLight.Command;
using ProtonVPN.Common.Vpn;
using ProtonVPN.Core.Modals;
using ProtonVPN.Core.MVVM;
using ProtonVPN.Core.Update;
using ProtonVPN.Core.Vpn;
using ProtonVPN.Resources;
using System;
using System.Dynamic;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;

namespace ProtonVPN.About
{
    public class UpdateViewModel : ViewModel, IUpdateStateAware, IVpnStateAware
    {
        private readonly UpdateService _updateService;
        private readonly IModals _modals;
        private readonly IDialogs _dialogs;

        private VpnStatus _vpnStatus;

        public UpdateViewModel(UpdateService updateService, IModals modals, IDialogs dialogs)
        {
            _updateService = updateService;
            _modals = modals;
            _dialogs = dialogs;

            OpenAboutCommand = new RelayCommand(OpenAbout);
        }

        private RelayCommand _updateCommand;
        public ICommand UpdateCommand => _updateCommand ?? (_updateCommand = new RelayCommand(Update, CanUpdate));

        public ICommand OpenAboutCommand { get; }

        private UpdateStatus _status;
        public UpdateStatus Status
        {
            get => _status;
            set => Set(ref _status, value);
        }

        private bool _available;
        public bool Available
        {
            get => _available;
            set => Set(ref _available, value);
        }

        private bool _ready;
        public bool Ready
        {
            get => _ready;
            set
            {
                Set(ref _ready, value);
                _updateCommand?.RaiseCanExecuteChanged();
            }
        }

        private bool _updating;
        public bool Updating
        {
            get => _updating;
            set
            {
                Set(ref _updating, value);
                _updateCommand?.RaiseCanExecuteChanged();
            }
        }

        public void OnUpdateStateChanged(UpdateStateChangedEventArgs e)
        {
            Status = e.Status;
            Available = e.Available;
            Ready = e.Ready;
        }

        public Task OnVpnStateChanged(VpnStateChangedEventArgs e)
        {
            _vpnStatus = e.State.Status;
            return Task.CompletedTask;
        }

        private void Update()
        {
            if (!CanUpdate() || !AllowToDisconnect())
            {
                return;
            }

            Updating = true;
            _updateService.Update(false).ContinueWith(t =>
            {
                Application.Current.Dispatcher.BeginInvoke((Action)(() => Updating = false));
            });
        }

        private bool CanUpdate() => !Updating && Ready;

        private void OpenAbout()
        {
            dynamic options = new ExpandoObject();
            options.SkipUpdateCheck = true;
            _modals.Show<AboutModalViewModel>(options);
        }

        private bool AllowToDisconnect()
        {
            if (_vpnStatus != VpnStatus.Disconnected && _vpnStatus != VpnStatus.Disconnecting)
            {
                var result = _dialogs.ShowQuestion(StringResources.Get("App_msg_UpdateConnectedConfirm"));
                if (result.HasValue && result.Value == false)
                {
                    return false;
                }
            }

            return true;
        }
    }
}
