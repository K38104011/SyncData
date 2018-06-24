using System;
using System.Collections.Generic;
using System.Linq;
using Autofac;
using FluentValidation;
using Newtonsoft.Json;
using System.Reflection;

namespace SyncData
{
    public interface IBusinessModelValidation
    {
        string CompanyCode { get; set; }
    }

    public interface IModelValidation<TBusinessModelValidation, TDataTypeModelValidation, TModel> : IBaseModelValidation
        where TBusinessModelValidation : IBusinessModelValidation
        where TModel : class
        where TDataTypeModelValidation : TModel
    {
        IEnumerable<IValidationResult> ValidateBusiness(
            TBusinessModelValidation validationBusinessModel);
        IEnumerable<IValidationResult> ValidateDataType(
        TBusinessModelValidation validationBusinessModel);
    }

    public interface IBaseModelValidation
    {
        object Result { get; set; }
        IEnumerable<string> Validate();
    }

    public abstract class JsonModelValidation<TBusinessModelValidation, TDataTypeModelValidation, TModel> :
        IModelValidation<TBusinessModelValidation, TDataTypeModelValidation, TModel>
            where TBusinessModelValidation : IBusinessModelValidation
            where TModel : class
            where TDataTypeModelValidation : TModel
    {
        private IEnumerable<TBusinessModelValidation> _validationBusinessModels;
        private readonly IModelValidationHelper _modelValidationHelper;
        public object Result { get; set; }

        protected JsonModelValidation(IModelValidationHelper modelValidationHelper, string inputData)
        {
            _modelValidationHelper = modelValidationHelper;
            var modelType = typeof(TBusinessModelValidation);
            var modelListType = typeof(List<>).MakeGenericType(modelType);
            _validationBusinessModels = (IEnumerable<TBusinessModelValidation>) 
                JsonConvert.DeserializeObject(inputData, modelListType);
        }

        protected JsonModelValidation(string inputData) :
            this(new DefaultModelValidationHelper(), inputData)
        {
        }

        public virtual IEnumerable<IValidationResult> ValidateBusiness(
            TBusinessModelValidation validationBusinessModel)
        {
            var validatorName = $"{typeof(TModel).Name}Validator";
            var validatorType = AppDomain.CurrentDomain.GetAssemblies()
                       .SelectMany(t => t.GetTypes())
                       .Where(t =>  t.IsClass &&
                                    t.Namespace == "SyncData" &&
                                    t.Name == validatorName)
                        .First();
            var instanceValidator = (IValidator) Activator.CreateInstance(validatorType);
            var validationResult = instanceValidator.Validate(validationBusinessModel);
            var result = validationResult.Errors.Select(e => {
                return new ValidationResult{
                    ErrorCode = e.ErrorCode,
                    PropertyName = e.PropertyName
                };
            });
            return result;
        }

        public virtual IEnumerable<IValidationResult> ValidateDataType(
            TBusinessModelValidation validationBusinessModel)
        {
            var type = typeof(TDataTypeModelValidation);
            return new List<ValidationResult>();
        }

        public virtual IEnumerable<string> Validate()
        {
            IEnumerable<IValidationResult> validationResults = null;
            foreach (var model in _validationBusinessModels)
            {
                var validationBusinessResults = ValidateBusiness(model);
                if (validationBusinessResults.Any())
                {
                    validationBusinessResults = validationBusinessResults
                        .Union(ValidateDataType(model));
                    return _modelValidationHelper.BuildValidationResultMessage(validationBusinessResults);
                }
            }
            Result = BuildResult(new ModelValidationContext
            {
                ValidationResults = validationResults,
                BusinessModels = _validationBusinessModels
            });
            return new List<string>();
        }

        protected abstract object BuildResult(ModelValidationContext context);

        protected object DefaultBuildResult(ModelValidationContext context)
        {
            return null;
        }

        public class ValidationResult : IValidationResult
        {
            public string ErrorCode { get; set; }
            public string PropertyName { get; set; }
        }

        public class ModelValidationContext
        {
            public IEnumerable<IValidationResult> ValidationResults { get; set; }
            public IEnumerable<TBusinessModelValidation> BusinessModels { get; set; }
        }
    }

    public class DefaultModelValidationHelper : IModelValidationHelper
    {
        public IEnumerable<string> BuildValidationResultMessage(IEnumerable<IValidationResult> validationResults)
        {
            return new List<string>();
        }
    }

    public class CustomModelValidationHelper : IModelValidationHelper
    {
        public IEnumerable<string> BuildValidationResultMessage(IEnumerable<IValidationResult> validationResults)
        {
            return new List<string>();
        }
    }

    public interface IModelValidationHelper
    {
        IEnumerable<string> BuildValidationResultMessage(IEnumerable<IValidationResult> validationResults);
    }

    public interface IValidationResult
    {
        string ErrorCode { get; set; }
        string PropertyName { get; set; }
    }

    public class Region
    {
        public string Name { get; set; }
    }

    public class RegionDataType : Region
    {
        public string AnotherName { get; set; }
    }

    public class RegionBusiness : Region, IBusinessModelValidation
    {
        public string CompanyCode { get; set; }
    }

    public class RegionModelValidation : JsonModelValidation<RegionBusiness, RegionDataType, Region>
    {
        public RegionModelValidation(string inputData) : base(inputData)
        {

        }

        public RegionModelValidation(IModelValidationHelper modelValidationHelper, string inputData)
            : base(modelValidationHelper, inputData)
        {

        }

        protected override object BuildResult(ModelValidationContext context)
        {
            return base.DefaultBuildResult(context);
        }
    }

    public class RegionValidator : AbstractValidator<Region>
    {
        public RegionValidator()
        {
            RuleFor(e => e.Name).NotNull().NotEmpty();
        }
    }

    class Program
    {
        static void Main(string[] args)
        {
            var builder = new ContainerBuilder();
            builder.RegisterType<DefaultModelValidationHelper>().As<IModelValidationHelper>();
            builder.RegisterType<RegionModelValidation>()
                    .Named<IBaseModelValidation>(nameof(RegionModelValidation));
            var container = builder.Build();

            using (var scope = container.BeginLifetimeScope())
            {
                var json = @"[{ Name: 'Giang' }]";
                var validation = scope
                    .ResolveNamed<IBaseModelValidation>(
                        nameof(RegionModelValidation), new PositionalParameter(1, json));
                var validationResults = validation.Validate();
                if (validationResults.Any())
                {
                    throw new Exception(string.Join(",", validationResults));
                }
                else
                {
                    var records = (IEnumerable<Region>)validation.Result;
                }
            }

            Console.WriteLine("Hello World!");
        }
    }
}
