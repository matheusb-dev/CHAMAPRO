using System.Diagnostics;
using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Net.Sockets;
using System.Security.Cryptography;
using System.Text.Json;
using System.Text.Json.Nodes;
using Chamapro.Api.Data;
using Chamapro.Api.Models.Entities;
using Chamapro.Api.Models.Enums;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Npgsql;
using Npgsql.NameTranslation;

const string targetDatabase="chamapro_homologacao_20261104";
var artifactRoot=Path.GetFullPath("Tests/.runtime/persistence");
var apiAssembly=Path.GetFullPath("Tests/.artifacts/bin/Chamapro.Api/debug/Chamapro.Api.dll");
const string postgresBin="C:/Program Files/PostgreSQL/18/bin/";
const string testAddress="http://127.0.0.1:5097/";
string stage="guards";
Process? server=null;
try {
    if(Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT")!="Development"||args.Length!=1||args[0] is not ("--prepare" or "--prepare-empty" or "--run"))throw new Exception("ExplicitModeRequired");
    var config=new ConfigurationBuilder().SetBasePath(Directory.GetCurrentDirectory()).AddJsonFile("appsettings.json").AddJsonFile("appsettings.Development.json",true).AddUserSecrets(typeof(AppDbContext).Assembly,true).AddEnvironmentVariables().Build();
    var source=new NpgsqlConnectionStringBuilder(config.GetConnectionString("DefaultConnection"));
    if(source.Host is not("localhost" or "127.0.0.1" or "::1")||source.Port!=5432||source.Database!="chamapro_local")throw new Exception("SourceGuard");
    var isolated=new NpgsqlConnectionStringBuilder(source.ConnectionString){Database=targetDatabase};
    await using var origin=new NpgsqlConnection(source.ConnectionString);await origin.OpenAsync();
    async Task<long> Scalar(NpgsqlConnection conn,string sql){await using var cmd=new NpgsqlCommand(sql,conn);return Convert.ToInt64(await cmd.ExecuteScalarAsync());}
    var sourceBefore=await Scalar(origin,"SELECT (SELECT count(*) FROM usuarios)*1000000+(SELECT count(*) FROM pedidos)*1000+(SELECT count(*) FROM orcamentos)");
    Directory.CreateDirectory(artifactRoot);
    var markerFile=Path.Combine(artifactRoot,"isolated-marker.json");var schemaFile=Path.Combine(artifactRoot,"schema-only.sql");
    async Task Pg(string program,NpgsqlConnectionStringBuilder cs,params string[] arguments){var psi=new ProcessStartInfo(Path.Combine(postgresBin,program)){UseShellExecute=false,CreateNoWindow=true,RedirectStandardOutput=true,RedirectStandardError=true};foreach(var arg in arguments)psi.ArgumentList.Add(arg);psi.Environment["PGHOST"]=cs.Host;psi.Environment["PGPORT"]=cs.Port.ToString();psi.Environment["PGDATABASE"]=cs.Database;psi.Environment["PGUSER"]=cs.Username;if(!string.IsNullOrEmpty(cs.Password))psi.Environment["PGPASSWORD"]=cs.Password;if(!string.IsNullOrEmpty(cs.Passfile))psi.Environment["PGPASSFILE"]=cs.Passfile;using var proc=Process.Start(psi)!;var output=proc.StandardOutput.ReadToEndAsync();var error=proc.StandardError.ReadToEndAsync();await proc.WaitForExitAsync();await Task.WhenAll(output,error);if(proc.ExitCode!=0){await File.WriteAllTextAsync(Path.Combine(artifactRoot,"postgres-tool-error.txt"),await error);throw new Exception("PostgresToolFailed");}}
    if(args[0] is "--prepare" or "--prepare-empty"){
        stage="prepare-new-database";
        if(File.Exists(markerFile))throw new Exception("ExistingMarkerRefused");
        var exists=await Scalar(origin,"SELECT count(*) FROM pg_database WHERE datname='chamapro_homologacao_20261104'");
        if(args[0]=="--prepare"){
            if(exists!=0)throw new Exception("ExistingTargetRefused");
            await using var create=new NpgsqlCommand("CREATE DATABASE \"chamapro_homologacao_20261104\"",origin);await create.ExecuteNonQueryAsync();
        }else{
            if(exists!=1)throw new Exception("PreparedDatabaseMissing");
            await using var empty=new NpgsqlConnection(isolated.ConnectionString);await empty.OpenAsync();
            if(await Scalar(empty,"SELECT count(*) FROM pg_tables WHERE schemaname NOT IN ('pg_catalog','information_schema') AND schemaname NOT LIKE 'pg_toast%'")!=0)throw new Exception("AdoptOnlyEmptyDatabase");
        }
        await Pg("pg_dump.exe",source,"--schema-only","--no-owner","--no-privileges","--file",schemaFile);
        var hash=Convert.ToHexString(SHA256.HashData(await File.ReadAllBytesAsync(schemaFile)));
        await File.WriteAllTextAsync(markerFile,JsonSerializer.Serialize(new{database=targetDatabase,schemaSha256=hash,schemaApplied=false}));
        Console.WriteLine("PREPARED: base isolada vazia confirmada; somente esquema exportado para revisão. Sem restaurar esquema, iniciar API ou criar usuários. Arquivo: stage7-persistence/schema-only.sql.");
        return;
    }
    stage="reviewed-schema-restore";
    if(!File.Exists(markerFile))throw new Exception("MarkerMissing");
    var marker=JsonNode.Parse(await File.ReadAllTextAsync(markerFile))!.AsObject();
    if(marker["database"]!.GetValue<string>()!=targetDatabase||marker["schemaSha256"]!.GetValue<string>()!=Convert.ToHexString(SHA256.HashData(await File.ReadAllBytesAsync(schemaFile))))throw new Exception("MarkerMismatch");
    await using(var conn=new NpgsqlConnection(isolated.ConnectionString)){
        await conn.OpenAsync();await using var command=new NpgsqlCommand("SELECT current_database()",conn);if((string?)await command.ExecuteScalarAsync()!=targetDatabase)throw new Exception("TargetGuard");
        if(marker["schemaApplied"]!.GetValue<bool>()==false){if(await Scalar(conn,"SELECT count(*) FROM pg_tables WHERE schemaname='public'")!=0)throw new Exception("TargetNotEmpty");await Pg("psql.exe",isolated,"--single-transaction","--set","ON_ERROR_STOP=1","--file",schemaFile);marker["schemaApplied"]=true;await File.WriteAllTextAsync(markerFile,marker.ToJsonString());}
    }
    var translator=new NpgsqlNullNameTranslator();
    var options=new DbContextOptionsBuilder<AppDbContext>().UseNpgsql(isolated.ConnectionString,p=>{p.MapEnum<TipoUsuario>("tipo_usuario",nameTranslator:translator);p.MapEnum<StatusPedido>("status_pedido",nameTranslator:translator);p.MapEnum<StatusAgendamento>("status_agendamento",nameTranslator:translator);p.MapEnum<StatusVerificacao>("status_verificacao",nameTranslator:translator);}).Options;
    await using var db=new AppDbContext(options);
    if(await db.Usuarios.AnyAsync(u=>!u.Nome.StartsWith("[TESTE ETAPA7]")||u.Tipo==TipoUsuario.Admin))throw new Exception("ForeignDataOrAdminRefused");
    stage="start-owned-isolated-api";
    var listener=new TcpListener(IPAddress.Loopback,5097);listener.Start();listener.Stop();
    var start=new ProcessStartInfo("dotnet"){UseShellExecute=false,CreateNoWindow=true,RedirectStandardOutput=true,RedirectStandardError=true,WorkingDirectory=Directory.GetCurrentDirectory()};start.ArgumentList.Add(apiAssembly);start.ArgumentList.Add("--urls");start.ArgumentList.Add(testAddress.TrimEnd('/'));
    start.Environment["ASPNETCORE_ENVIRONMENT"]="Development";start.Environment["ConnectionStrings__DefaultConnection"]=isolated.ConnectionString;start.Environment["Jwt__Key"]=Convert.ToBase64String(RandomNumberGenerator.GetBytes(48));start.Environment["Bootstrap__AdminEmail"]="";start.Environment["Bootstrap__AdminPassword"]="";start.Environment["Arquivos__PastaPublica"]=Path.Combine(artifactRoot,"public");start.Environment["Arquivos__PastaPrivada"]=Path.Combine(artifactRoot,"private");start.Environment["Logging__LogLevel__Default"]="None";
    server=Process.Start(start)!;var logs=server.StandardOutput.ReadToEndAsync();var errors=server.StandardError.ReadToEndAsync();
    using var http=new HttpClient{BaseAddress=new Uri(testAddress+"api/v1/"),Timeout=TimeSpan.FromSeconds(20)};
    bool ready=false;for(int attempt=0;attempt<50;attempt++){if(server.HasExited)break;try{using var check=await http.GetAsync("categorias");if(check.IsSuccessStatusCode){ready=true;break;}}catch(HttpRequestException){}await Task.Delay(200);}if(!ready)throw new Exception("IsolatedApiNotReady");
    int checks=0;
    async Task<JsonElement> Request(string method,string route,object? body=null,int expected=0){using var req=new HttpRequestMessage(new HttpMethod(method),route);if(body is not null)req.Content=JsonContent.Create(body);using var response=await http.SendAsync(req);if(expected>0?(int)response.StatusCode!=expected:!response.IsSuccessStatusCode)throw new Exception("Http"+(int)response.StatusCode+"_"+stage);checks++;var text=await response.Content.ReadAsStringAsync();return string.IsNullOrWhiteSpace(text)?default:JsonDocument.Parse(text).RootElement.Clone();}
    void Session(string? token)=>http.DefaultRequestHeaders.Authorization=token is null?null:new AuthenticationHeaderValue("Bearer",token);
    void Verify(bool ok){if(!ok)throw new Exception("Assertion_"+stage);checks++;}
    string nonce=Guid.NewGuid().ToString("N")[..10],password=Convert.ToBase64String(RandomNumberGenerator.GetBytes(24));
    async Task<(string Token,int Id,string Email)> Register(string role,string suffix){Session(null);string email=$"{suffix}.{nonce}@etapa7.test.invalid";var data=await Request("POST","auth/cadastro/"+role,new{nome="[TESTE ETAPA7] "+suffix,email,senha=password,confirmacaoSenha=password,telefone="00000000000",descricaoPerfil="Apresentação fictícia para teste isolado."},201);return(data.GetProperty("token").GetString()!,data.GetProperty("usuario").GetProperty("id").GetInt32(),email);}
    stage="registration-and-login";
    var client=await Register("cliente","cliente1");var otherClient=await Register("cliente","cliente2");var pro=await Register("profissional","profissional1");var otherPro=await Register("profissional","profissional2");
    Session(null);var login=await Request("POST","auth/login",new{email=client.Email,senha=password},200);Verify(login.GetProperty("usuario").GetProperty("id").GetInt32()==client.Id);
    var category=new Categoria{Nome="[TESTE ETAPA7] Serviço "+nonce};db.Categorias.Add(category);db.Entry(category).Property(nameof(Categoria.Ativa)).CurrentValue=true;await db.SaveChangesAsync();
    // Fixture setup only in the newly created isolated DB; no admin account or administrative HTTP action.
    foreach(var id in new[]{pro.Id,otherPro.Id}){var account=await db.Usuarios.SingleAsync(u=>u.Id==id&&u.Nome.StartsWith("[TESTE ETAPA7]"));account.IdentidadeVerificada=true;}await db.SaveChangesAsync();
    var hours=Enumerable.Range(0,7).SelectMany(day=>new[]{new{diaSemana=day,horaInicio="00:00:00",horaFim="12:00:00",atendeImediato=true,atendeAgendado=true},new{diaSemana=day,horaInicio="12:00:00",horaFim="23:59:59.9999999",atendeImediato=true,atendeAgendado=true}}).ToArray();
    object Profile(string name,string description="Descrição profissional de teste isolado.")=>new{nome="[TESTE ETAPA7] "+name,telefone="00000000000",descricao=description,fotoUrl="https://example.invalid/perfil.png",fotosTrabalhos=new[]{"https://example.invalid/trabalho.png"},experienciaAnos=2,distanciaMaxKm=20,atendeUrgencia=true,atendeDomicilio=true,atendeEstabelecimento=false};
    async Task<int> Configure(string token,string name){Session(token);await Request("PUT","profissionais/me",Profile(name));await Request("PUT","profissionais/me/servicos",new[]{new{categoriaId=category.Id,faixaPrecoMin=0,faixaPrecoMax=150}});await Request("PUT","profissionais/me/areas",new[]{new{cidade="São Paulo",bairro=(string?)"Centro"},new{cidade="Santos",bairro=(string?)null}});await Request("PUT","profissionais/me/disponibilidade",hours,410);var profile=await Request("GET","profissionais/me");Verify(profile.GetProperty("servicos")[0].GetProperty("faixaPrecoMin").GetDecimal()==0);Verify(profile.GetProperty("areas").GetArrayLength()==2);Verify(profile.GetProperty("disponibilidade").GetArrayLength()==0);Verify(profile.GetProperty("fotosTrabalhos")[0].GetString()=="https://example.invalid/trabalho.png");return profile.GetProperty("id").GetInt32();}
    stage="profile-without-availability";int professionalId=await Configure(pro.Token,"profissional1"),otherProfessionalId=await Configure(otherPro.Token,"profissional2");
    Session(pro.Token);var before=await Request("GET","profissionais/me");await Request("PUT","profissionais/me",Profile("profissional1","Descrição alterada e preservada."));var after=await Request("GET","profissionais/me");Verify(after.GetProperty("descricao").GetString()=="Descrição alterada e preservada.");Verify(after.GetProperty("disponibilidade").GetRawText()==before.GetProperty("disponibilidade").GetRawText());Verify(after.GetProperty("servicos")[0].GetProperty("faixaPrecoMin").GetDecimal()==0);
    var panel=await Request("GET","painel/profissional");Verify(panel.GetProperty("cadastroCompleto").GetBoolean());
    stage="favorites";Session(client.Token);await Request("POST",$"favoritos/{professionalId}");Verify((await Request("GET","favoritos")).GetArrayLength()==1);Session(otherClient.Token);Verify((await Request("GET","favoritos")).GetArrayLength()==0);Session(client.Token);await Request("DELETE",$"favoritos/{professionalId}");Verify((await Request("GET","favoritos")).GetArrayLength()==0);
    object Order(bool auto,string city="São Paulo")=>new{categoriaId=category.Id,titulo="[TESTE ETAPA7] Pedido isolado",descricao="Descrição fictícia, sem atendimento real.",cidade=city,bairro="Centro",tipoAtendimento="IMEDIATO",distribuirParaCompativeis=auto,profissionalIds=auto?Array.Empty<int>():new[]{professionalId},fotoUrls=Array.Empty<string>()};
    stage="orders-and-compatibility";var automatic=await Request("POST","pedidos",Order(true),201);int orderId=automatic.GetProperty("id").GetInt32();Verify(automatic.GetProperty("destinatarios").GetArrayLength()==2);var manual=await Request("POST","pedidos",Order(false),201);Verify(manual.GetProperty("destinatarios").GetArrayLength()==1);
    int count=await db.Pedidos.CountAsync();var noMatches=await Request("POST","pedidos",Order(true,"Cidade sem fixture"),201);Verify(noMatches.GetProperty("destinatarios").GetArrayLength()==0);Verify(await db.Pedidos.CountAsync()==count+1);
    Session(otherClient.Token);await Request("GET",$"pedidos/{orderId}",expected:404);
    stage="proposal-interest-send-resend";Session(pro.Token);await Request("POST",$"pedidos/{orderId}/aceitar");var quote=await Request("POST",$"pedidos/{orderId}/orcamentos",new{valor=120,prazoEstimado="2 horas",condicoes="Condição fictícia"},201);int quoteId=quote.GetProperty("id").GetInt32();var resent=await Request("POST",$"pedidos/{orderId}/orcamentos",new{valor=130,prazoEstimado="3 horas",condicoes="Atualizada"},201);Verify(resent.GetProperty("id").GetInt32()==quoteId);Verify((await Request("GET",$"pedidos/{orderId}")).GetProperty("meuOrcamento").GetProperty("valor").GetDecimal()==130);
    Session(otherPro.Token);await Request("POST",$"pedidos/{orderId}/aceitar");await Request("POST",$"pedidos/{orderId}/orcamentos",new{valor=140,prazoEstimado="4 horas"},201);
    stage="proposal-choice-and-ownership";Session(client.Token);Verify((await Request("GET",$"pedidos/{orderId}/orcamentos")).GetArrayLength()==2);await Request("POST",$"orcamentos/{quoteId}/aceitar");var selected=await Request("GET",$"pedidos/{orderId}");Verify(selected.GetProperty("profissionalId").GetInt32()==professionalId);Verify(selected.GetProperty("status").GetString()=="ACEITO");Session(otherPro.Token);await Request("PATCH",$"pedidos/{orderId}/status",new{status="EM_ANDAMENTO"},403);
    stage="urgent-execution-review";Session(pro.Token);await Request("POST","agendamentos",new{pedidoId=orderId,dataHora=DateTime.UtcNow.AddDays(1)},410);await Request("PATCH",$"pedidos/{orderId}/status",new{status="EM_ANDAMENTO"});Verify((await Request("GET",$"pedidos/{orderId}")).GetProperty("status").GetString()=="EM_ANDAMENTO");await Request("PATCH",$"pedidos/{orderId}/status",new{status="CONCLUIDO"});Verify((await Request("GET",$"pedidos/{orderId}")).GetProperty("status").GetString()=="CONCLUIDO");Session(client.Token);await Request("POST","avaliacoes",new{pedidoId=orderId,nota=5,comentario="Teste isolado"});Verify((await Request("GET",$"pedidos/{orderId}")).GetProperty("avaliacao").GetProperty("nota").GetInt32()==5);await Request("POST","avaliacoes",new{pedidoId=orderId,nota=5},409);
    stage="partial-save-observation";Session(pro.Token);await Request("PUT","profissionais/me/areas",new[]{new{cidade="São Paulo",bairro="Centro"}});await Request("PUT","profissionais/me",Profile("profissional1",""),400);var partial=await Request("GET","profissionais/me");Verify(partial.GetProperty("areas").GetArrayLength()==1);Verify(partial.GetProperty("descricao").GetString()=="Descrição alterada e preservada.");Verify(partial.GetProperty("disponibilidade").GetArrayLength()==0);Verify(partial.GetProperty("servicos")[0].GetProperty("faixaPrecoMin").GetDecimal()==0);
    stage="private-file-ownership";using(var form=new MultipartFormDataContent()){var pdf=new ByteArrayContent(System.Text.Encoding.UTF8.GetBytes("%PDF-1.4\n[TESTE ETAPA7] documento ficticio\n%%EOF"));pdf.Headers.ContentType=new MediaTypeHeaderValue("application/pdf");form.Add(pdf,"arquivo","ficticio.pdf");form.Add(new StringContent("true"),"privado");using var uploaded=await http.PostAsync("arquivos",form);Verify(uploaded.IsSuccessStatusCode);var data=await uploaded.Content.ReadFromJsonAsync<JsonElement>();var uri=new Uri(data.GetProperty("url").GetString()!);Verify(uri.GetLeftPart(UriPartial.Authority)==testAddress.TrimEnd('/'));using var ownFile=await http.GetAsync(uri);Verify(ownFile.IsSuccessStatusCode&&ownFile.Content.Headers.ContentType?.MediaType=="application/pdf"&&ownFile.Headers.CacheControl?.NoStore==true);Session(otherPro.Token);using var denied=await http.GetAsync(uri);Verify((int)denied.StatusCode==404);Session(null);using var anonymous=await http.GetAsync(uri);Verify((int)anonymous.StatusCode==401);}
    stage="final-guards";Verify(!await db.Usuarios.AnyAsync(u=>u.Tipo==TipoUsuario.Admin));Verify(sourceBefore==await Scalar(origin,"SELECT (SELECT count(*) FROM usuarios)*1000000+(SELECT count(*) FROM pedidos)*1000+(SELECT count(*) FROM orcamentos)"));
    marker["lastResult"]="PASS";marker["lastChecks"]=checks;await File.WriteAllTextAsync(markerFile,marker.ToJsonString());
    Console.WriteLine($"PASS PERSISTÊNCIA ISOLADA: {checks} requisições/asserções com GET após gravação. Cadastro/login, perfil, preço zero, ausência de horários, regiões, favoritos, pedido automático/manual, ausência de compatíveis, proposta/reenvio/escolha, recusa de agenda antiga, início/conclusão/avaliação, salvamento parcial e documento privado.");
    Console.WriteLine("Base exclusiva chamapro_homologacao_20261104 preservada para revisão. Sem administrador, migrations, limpeza ou alteração dos registros da demonstração. Instância de teste encerrada ao terminar. Credenciais e tokens não exibidos.");
}catch(Exception error){Console.Error.WriteLine($"FAIL teste isolado na etapa {stage}: {error.GetType().Name}, SQLSTATE={(error is PostgresException pg?pg.SqlState:"n/a")}. Código seguro: {(System.Text.RegularExpressions.Regex.IsMatch(error.Message,"^[A-Za-z0-9_-]{1,100}$")?error.Message:"indisponivel")}. Base e artefatos preservados; nenhum segredo foi exibido.");Environment.ExitCode=1;}
finally{if(server is not null){try{if(!server.HasExited){server.Kill(true);await server.WaitForExitAsync();}}finally{server.Dispose();}}}
