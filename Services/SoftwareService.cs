using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using ShelfApi.Models;
using ShelfApi.Repositories;

namespace ShelfApi.Services;

public class SoftwareService : ISoftwareService
{
    private readonly ISoftwareRepository _softwareRepository;

    public SoftwareService(ISoftwareRepository softwareRepository)
    {
        _softwareRepository = softwareRepository;
    }

    public async Task<IEnumerable<Software>> GetAllAsync()
    {
        return await _softwareRepository.GetAllAsync();
    }

    public async Task<Software?> GetByIdAsync(int id)
    {
        return await _softwareRepository.GetByIdAsync(id);
    }

    public async Task<int> CreateAsync(Software software)
    {
        if (string.IsNullOrWhiteSpace(software.NomeSoftware))
            throw new ArgumentException("O nome do software é obrigatório.");

        if (string.IsNullOrWhiteSpace(software.Versao))
            throw new ArgumentException("A versão do software é obrigatória.");

        if (string.IsNullOrWhiteSpace(software.Cliente))
            throw new ArgumentException("O nome do cliente é obrigatório.");

        if (software.DataInstalacao == default)
        {
            software.DataInstalacao = DateTime.UtcNow;
        }

        return await _softwareRepository.CreateAsync(software);
    }

    public async Task<bool> UpdateAsync(int id, Software software)
    {
        if (string.IsNullOrWhiteSpace(software.NomeSoftware))
            throw new ArgumentException("O nome do software é obrigatório.");

        if (string.IsNullOrWhiteSpace(software.Versao))
            throw new ArgumentException("A versão do software é obrigatória.");

        if (string.IsNullOrWhiteSpace(software.Cliente))
            throw new ArgumentException("O nome do cliente é obrigatório.");

        software.Id = id;
        return await _softwareRepository.UpdateAsync(software);
    }

    public async Task<bool> DeleteAsync(int id)
    {
        return await _softwareRepository.DeleteAsync(id);
    }
}
