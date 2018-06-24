using System;
using System.Collections.Generic;
using System.Linq;

namespace SyncData
{
    interface IValidationBusinessModel
    {
        string CompanyCode { get; set; }
    }

    interface IModelValidation<TBusinessModelValidation, TDataTypeModelValidation, TModel>
        where TBusinessModelValidation : IValidationBusinessModel
        where TModel : class
        where TDataTypeModelValidation : TModel
    {
        IEnumerable<IValidationResult> ValidateBusiness(
            TBusinessModelValidation validationBusinessModel);
        IEnumerable<IValidationResult> ValidateDataType(
        TBusinessModelValidation validationBusinessModel);
        IEnumerable<string> Validate();
    }

    abstract class BaseModelValidation<TBusinessModelValidation, TDataTypeModelValidation, TModel> :
        IModelValidation<TBusinessModelValidation, TDataTypeModelValidation, TModel>
            where TBusinessModelValidation : IValidationBusinessModel
            where TModel : class
            where TDataTypeModelValidation : TModel
    {
        private IEnumerable<TBusinessModelValidation> _validationBusinessModels;
        private readonly IModelValidationHelper _modelValidationHelper;
        public IEnumerable<TModel> Models;

        protected BaseModelValidation(string inputData)
        {
            _validationBusinessModels = new List<TBusinessModelValidation>();
            _modelValidationHelper = new ModelValidationHelper();
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

    public class ModelValidationHelper : IModelValidationHelper
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

    class RegionBusiness : IValidationBusinessModel
    {
        public string CompanyCode { get; set; }
    }

    class RegionModelValidation : BaseModelValidation<RegionBusiness, RegionDataType, Region>
    {
        public RegionModelValidation(string inputData) : base(inputData)
        {

        }
    }

    class Program
    {
        static void Main(string[] args)
        {
            var regionModelValidation = new RegionModelValidation("test");
            var validationResults = regionModelValidation.Validate();
            if (validationResults.Any())
            {
                throw new Exception(string.Join(",", validationResults));
            }
            else {
                var records = regionModelValidation.Models;
            }
            Console.WriteLine("Hello World!");
        }
    }
}
