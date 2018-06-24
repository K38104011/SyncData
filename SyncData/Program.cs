using System;
using System.Collections.Generic;
using System.Linq;
using Autofac;

namespace SyncData
{
    interface IBusinessModelValidation
    {
        string CompanyCode { get; set; }
    }

    interface IModelValidation<TBusinessModelValidation, TDataTypeModelValidation, TModel> : IBaseModelValidation
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
        IEnumerable<object> Models { get; set; }
        IEnumerable<string> Validate();
    }

    abstract class JsonModelValidation<TBusinessModelValidation, TDataTypeModelValidation, TModel> :
        IModelValidation<TBusinessModelValidation, TDataTypeModelValidation, TModel>
            where TBusinessModelValidation : IBusinessModelValidation
            where TModel : class
            where TDataTypeModelValidation : TModel
    {
        private IEnumerable<TBusinessModelValidation> _validationBusinessModels;
        private readonly IModelValidationHelper _modelValidationHelper;
        public IEnumerable<object> Models { get; set; }

        protected JsonModelValidation(IModelValidationHelper modelValidationHelper, string inputData)
        {
            _modelValidationHelper = modelValidationHelper;
            _validationBusinessModels = new List<TBusinessModelValidation>();
        }

        protected JsonModelValidation(string inputData) :
            this(new DefaultModelValidationHelper(), inputData)
        {
        }

        public virtual IEnumerable<IValidationResult> ValidateBusiness(
            TBusinessModelValidation validationBusinessModel)
        {
            return new List<ValidationResult>();
        }

        public virtual IEnumerable<IValidationResult> ValidateDataType(
            TBusinessModelValidation validationBusinessModel)
        {
            var type = typeof(TDataTypeModelValidation);
            return new List<ValidationResult>();
        }

        public virtual IEnumerable<string> Validate()
        {
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
            Models = new List<TModel>();
            return new List<string>();
        }

        public class ValidationResult : IValidationResult
        {
            public string ErrorCode { get; set; }
            public string PropertyName { get; set; }
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

    class Region
    {
        public string Name { get; set; }
    }

    class RegionDataType : Region
    {
        public string AnotherName { get; set; }
    }

    class RegionBusiness : IBusinessModelValidation
    {
        public string CompanyCode { get; set; }
    }

    class RegionModelValidation : JsonModelValidation<RegionBusiness, RegionDataType, Region>
    {
        public RegionModelValidation(string inputData) : base(inputData)
        {

        }

        public RegionModelValidation(IModelValidationHelper modelValidationHelper, string inputData)
            : base(modelValidationHelper, inputData)
        {

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
                var validation = scope
                    .ResolveNamed<IBaseModelValidation>(
                        nameof(RegionModelValidation), new PositionalParameter(1, "test"));
                var validationResults = validation.Validate();
                if (validationResults.Any())
                {
                    throw new Exception(string.Join(",", validationResults));
                }
                else
                {
                    var records = validation.Models;
                }
            }

            Console.WriteLine("Hello World!");
        }
    }
}
