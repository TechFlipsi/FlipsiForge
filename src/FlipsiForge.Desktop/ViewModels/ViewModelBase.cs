// SPDX-License-Identifier: GPL-3.0-or-later
using System.ComponentModel;
using System.Runtime.CompilerServices;
using CommunityToolkit.Mvvm.ComponentModel;

namespace FlipsiForge.Desktop.ViewModels;

/// <summary>
/// Basis-ViewModel für alle FlipsiForge Desktop-ViewModels. Stellt
/// <see cref="INotifyPropertyChanged"/>-Infrastruktur bereit und erbt von
/// <see cref="ObservableObject"/> aus CommunityToolkit.Mvvm, sodass
/// [ObservableProperty]-Attribute automatisch funktionieren.
/// </summary>
public abstract class ViewModelBase : ObservableObject
{
    /// <summary>Setzt eine Property und benachrichtigt Listener nur bei Änderung.</summary>
    protected bool Set<T>(ref T field, T value, [CallerMemberName] string? name = null)
        => SetProperty(ref field, value, name);
}