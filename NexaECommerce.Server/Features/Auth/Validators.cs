using FluentValidation;

namespace NexaECommerce.Server.Features.Auth;

//Mirrors Identity's password policy (length ≥ 8) so the client gets clean 400 field errors
//before UserManager re-checks server-side.
internal sealed class RegisterRequestValidator
    : AbstractValidator<RegisterRequest>
{
    public RegisterRequestValidator()
    {
        RuleFor(x => x.Identifier)
            .Cascade(CascadeMode.Stop)
            .NotEmpty()
            .Must(value => AuthIdentifier.TryParse(value, out _))
            .WithMessage(
                "Enter a valid email address or mobile number.");

        RuleFor(x => x.Password)
            .NotEmpty()
            .MinimumLength(6)
            .MaximumLength(128);

        RuleFor(x => x.DisplayName)
            .MaximumLength(100);
    }
}

internal sealed class LoginRequestValidator
    : AbstractValidator<LoginRequest>
{
    public LoginRequestValidator()
    {
        RuleFor(x => x.Identifier)
            .Cascade(CascadeMode.Stop)
            .NotEmpty()
            .Must(value => AuthIdentifier.TryParse(value, out _))
            .WithMessage(
                "Enter a valid email address or mobile number.");

        RuleFor(x => x.Password)
            .NotEmpty();
    }
}

internal sealed class ForgotPasswordRequestValidator : AbstractValidator<ForgotPasswordRequest>
{
    public ForgotPasswordRequestValidator() => RuleFor(x => x.Email).NotEmpty().EmailAddress();
}

internal sealed class ResendConfirmationRequestValidator : AbstractValidator<ResendConfirmationRequest>
{
    public ResendConfirmationRequestValidator() => RuleFor(x => x.Email).NotEmpty().EmailAddress();
}

internal sealed class ResetPasswordRequestValidator : AbstractValidator<ResetPasswordRequest>
{
    public ResetPasswordRequestValidator()
    {
        RuleFor(x => x.Email).NotEmpty().EmailAddress();
        RuleFor(x => x.Token).NotEmpty();
        RuleFor(x => x.NewPassword).NotEmpty().MinimumLength(6).MaximumLength(128);
    }
}

internal sealed class ChangePasswordRequestValidator : AbstractValidator<ChangePasswordRequest>
{
    public ChangePasswordRequestValidator()
    {
        //CurrentPassword isn't required: an OAuth-created account has no password yet, so this same request
        //sets the first one. The handler verifies the current password only when the account already has one.
        RuleFor(x => x.NewPassword).NotEmpty().MinimumLength(6).MaximumLength(128);
    }
}

internal sealed class UpdateProfileRequestValidator : AbstractValidator<UpdateProfileRequest>
{
    public UpdateProfileRequestValidator() => RuleFor(x => x.DisplayName).MaximumLength(100);
}

internal sealed class UpdatePreferencesRequestValidator : AbstractValidator<UpdatePreferencesRequest>
{
    public UpdatePreferencesRequestValidator()
    {
        RuleFor(x => x.Locale).MaximumLength(16);
        RuleFor(x => x.TimeZone).MaximumLength(100);
    }
}

internal sealed class EnableTwoFactorRequestValidator : AbstractValidator<EnableTwoFactorRequest>
{
    public EnableTwoFactorRequestValidator() => RuleFor(x => x.Code).NotEmpty();
}

internal sealed class TwoFactorLoginRequestValidator : AbstractValidator<TwoFactorLoginRequest>
{
    public TwoFactorLoginRequestValidator() => RuleFor(x => x.Code).NotEmpty();
}

internal sealed class RecoveryCodeLoginRequestValidator : AbstractValidator<RecoveryCodeLoginRequest>
{
    public RecoveryCodeLoginRequestValidator() => RuleFor(x => x.RecoveryCode).NotEmpty();
}
