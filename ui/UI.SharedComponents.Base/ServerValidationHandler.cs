using Microsoft.AspNetCore.Components.Forms;

namespace UI.SharedComponents.Base;

public sealed class ServerValidationHandler
{
    private readonly EditContext _editContext;
    private readonly ValidationMessageStore _messageStore;

    public ServerValidationHandler(EditContext editContext)
    {
        _editContext = editContext;
        _messageStore = new ValidationMessageStore(editContext);

        _editContext.OnFieldChanged += (_, e) =>
            _messageStore.Clear(e.FieldIdentifier);
    }

    public void DisplayErrors(Dictionary<string, string[]> errors)
    {
        _messageStore.Clear();

        foreach (var (fieldName, messages) in errors)
        {
            var fieldIdentifier = _editContext.Field(fieldName);

            foreach (var message in messages)
                _messageStore.Add(fieldIdentifier, message);
        }

        _editContext.NotifyValidationStateChanged();
    }

    public void Clear()
    {
        _messageStore.Clear();
        _editContext.NotifyValidationStateChanged();
    }
}