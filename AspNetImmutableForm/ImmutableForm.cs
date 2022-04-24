using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Forms;
using Microsoft.AspNetCore.Components.Rendering;

namespace Components.Forms;

public class ImmutableForm<TFormModel> : ComponentBase
{
    private readonly Func<Task> _handleSubmitDelegate;
    private EditContext? _editContext;
    private TFormModel? _internalMutableModel;
    private TFormModel? _initialValues;

    public ImmutableForm()
        => _handleSubmitDelegate = HandleSubmitAsync;

#if NETSTANDARD2_1_OR_GREATER
        [NotNull]
#endif
    [Parameter]
    public TFormModel InitialValues { get => _initialValues ?? throw new InvalidOperationException($"{nameof(InitialValues)} cannot be null"); set => _initialValues = value; }
    [Parameter]
    public RenderFragment<TFormModel>? ChildContent { get; set; }
    [Parameter]
    public EventCallback<EditContext> OnSubmit { get; set; }
    [Parameter]
    public EventCallback<EditContext> OnValidSubmit { get; set; }
    [Parameter]
    public EventCallback<EditContext> OnInvalidSubmit { get; set; }
    [Parameter(CaptureUnmatchedValues = true)]
    public IReadOnlyDictionary<string, object>? AdditionalAttributes { get; set; }

#if NET5_0_OR_GREATER
    [MemberNotNull(nameof(_editContext), nameof(_internalMutableModel))]
#endif
    protected override void OnParametersSet()
    {
        if (OnSubmit.HasDelegate && (OnValidSubmit.HasDelegate || OnInvalidSubmit.HasDelegate))
        {
            throw new InvalidOperationException($"When supplying an {nameof(OnSubmit)} parameter to " +
                $"{nameof(ImmutableForm<TFormModel>)}, do not also supply {nameof(OnValidSubmit)} or {nameof(OnInvalidSubmit)}.");
        }

        if (_editContext is null || _internalMutableModel is null || _editContext.Model is not TFormModel)
        {
            _internalMutableModel = InitialValues.DeepCopy()!;
            _editContext = new EditContext(_internalMutableModel);
        }
    }

    protected override void BuildRenderTree(RenderTreeBuilder builder)
    {
        if (_editContext is null)
        {
            throw new InvalidOperationException($"{nameof(_editContext)} must be set");
        }

        builder.OpenRegion(_editContext.GetHashCode());

        builder.OpenElement(0, "form");
        builder.AddMultipleAttributes(1, AdditionalAttributes);
        builder.AddAttribute(2, "onsubmit", _handleSubmitDelegate);
        builder.OpenComponent<CascadingValue<EditContext>>(3);
        builder.AddAttribute(4, "IsFixed", true);
        builder.AddAttribute(5, "Value", _editContext);
        builder.AddAttribute(6, "ChildContent", ChildContent?.Invoke(_internalMutableModel!));
        builder.CloseComponent();
        builder.CloseElement();

        builder.CloseRegion();
    }

    private async Task HandleSubmitAsync()
    {
        if (_editContext is null)
        {
            throw new InvalidOperationException($"{nameof(_editContext)} Must not be null");
        }

        if (OnSubmit.HasDelegate)
        {
            await OnSubmit.InvokeAsync(_editContext);
        }
        else
        {
            var isValid = _editContext.Validate();

            if (isValid && OnValidSubmit.HasDelegate)
            {
                await OnValidSubmit.InvokeAsync(_editContext);
            }

            if (!isValid && OnInvalidSubmit.HasDelegate)
            {
                await OnInvalidSubmit.InvokeAsync(_editContext);
            }
        }
    }
}