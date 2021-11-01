# **RESILIENCIA E SERVICE MESH**

Muito provavelmente você já ouviu falar na arquitetura de microsserviços. Essa arquitetura consiste em aplicações, com escopo delimitado e de responsabilidade única, que são distribuídas na rede para formar um sistema maior e mais robusto.Como consequência, de maior complexidade também. Saber como administrar e organizar este tipo de sistema é uma tarefa muito importante (e difícil) para obter o melhor desempenho e garantir a disponibilidade de toda a aplicação. Nesse post quero tratar de um ponto bastante relevante para o desenvolvimento de software: resiliência. Iremos abordar como podemos aplicar algumas tecninas em aplicações .Net e irei começar a falar sobre o Service Mesh, que trata muito bem desse assunto, podendo ir além.

## **RESILIENCIA**

De uma forma geral, resiliência pode ser entendio como: 

1. FÍSICA
	propriedade que alguns corpos apresentam de retornar à forma original após terem sido submetidos a uma deformação elástica.
2. FIGURADO (SENTIDO)•FIGURADAMENTE
	capacidade de se recobrar facilmente ou se adaptar à má sorte ou às mudanças.
	
Levando em consideração essas definições dentro de uma arquitetura de microsserviços, e até mesmo em arquiteturas que utilizam de APIs REST em sua estrutura, a resiliência significa que, uma vez que temos serviços independentes, estes não deveriam afetar outros serviços em casos de falha. Ser resiliente é aceitar que falhas irão acontecer. O restante da aplicação deveria permanecer em funcionamento, mesmo que limitadamente. Dessa forma, os sitesmas se tornam mais confiaveis ao tolerar certos tipos de problemas, uma vez que podem ser implementadas algumas tecnicas (e/ou design patterns) para garantir a resiliencia. A seguir vou descrever alguns deles e posteriormente iremos implementar algumas dessas abordagens.

* **Retry** - Possiblita a uma aplicação de tratar falhas ao tentar se conectar a um serviço indisponivel temporariamente. Seguindo a ideia de retentar uma nova conexão, uma vez que configuramos um tempo de espera (delay) antes de uma nova tentativa, o serviço que é solicitado pode se reestabelecer.
	
* **Circuit Breaker** - Garante que uma falha em sequencia não ocorra em chamadas de serviço a serviço. Ao tentar acessar um serviço indisponivel, ao final de algumas tentativas o circuito entra em um estado que não permite enviar novas tentativas, evitando falhas em cascata.
	
* **Timeout** - Muitos clientes HTTP já têm estabelecido um tempo limite padrão configurado. O objetivo é evitar que um serviço aguarde indeterminadamente por uma resposta.

## **Exemplo - Polly**

Para testarmos na prática os padrões de resiliência comentados acima, vamos criar algumas APIs com .NET 5, bem simples e que irão simular a comunicação entre microsserviços. Utilizaremos também o Polly. Polly é uma biblioteca para aplicações .NET que trata justamente a resiliencia de nossas aplicações.

Utilizando a CLI do dotnet criaremos três aplicações, seguindo os comandos abaixo:

```
dotnet new webapi -o App.A
dotnet new webapi -o App.B
dotnet new webapi -o App.C
```	
Após criado os arquivos em seus respectivos diretórios, precisamos agora instalar a biblioteca do Polly. Para nossa aplicação em .Net Core, vamos utilizar uma extensão da própria Microsoft, que já contem o que precisamos para utilizar o Polly em nossa API.

```
dotnet add package Microsoft.Extensions.Http.Polly
```
	
O comando acima adicionara o package do nuget em nossa aplicação. Lembre-se de executá-lo para cada uma das APIs.

Uma API do dotnet criada com os comandos acima, já vem com um exemplo template de uma aplicação de previsão de tempo. Basicamente existe um Model WheaterForecast que possui as informações do tempo e um Controller que expõe um endpoint via swagger com a listagem de algumas informações do tempo. Vamos quebrar esse exemplo entre o serviços a fim de obtermos o mesmo resultado da aplicação exemplo, mas agora as 3 aplicações trabalhando em conjunto.

Para isso, vamos fazer o seguinte:

* A App.C será responsável por apenas listar o Summaries (as informações sobre o tempo, como frio, calor, tempo humido, etc.);
* A App.B irá consumir a API da aplicação C, que poderá ser acessada para exibir a previsão do tempo, informando na sua consulta a data, as informações do tempo e a temperatura em graus Celsius;
* A App.A por sua vez, irá consumir a API B, mas ira exibir as informações convertidas para a escala de temperatura Fahrenheit.

Para isso fiz o seguinte (github);

passo a passo

Feito as alterações em cada serviço, resta realizar a injeção de dependências da nossa biblioteca de resiliência. Na classe Startup de cada aplicação (exceto a App.C), vamos adicionar o seguinte trecho de código:

```csharp
public void ConfigureServices(IServiceCollection services)
{
    services.AddHttpClient(IService, Service);

    // ...
}
```

Isso já é o suficiente para testar a aplicação localmente. Muito provavelmente você terá que alterar as portas de entrada da aplicação, pois inicialmente todas estarão configuradas para acessarem o swagger em https://localhost:5001 . Para corrigir, acesse o aquivo launchSettings.json, dentro da pasta Properties de cada aplicação, e altere a porta da url da aplicação. No meu caso ficou assim:
* App.A: "applicationUrl": "https://localhost:5001;http://localhost:5000"
* App.B: "applicationUrl": "https://localhost:5003;http://localhost:5002"
* App.C: "applicationUrl": "https://localhost:5005;http://localhost:5004"

Ao executar a aplicação com o comando "dotnet run" apontado corretamente pra cada aplicação, veremeos a documentação Swagger da aplicação A, B e C, exibindo as informações conforme esperado.

### **Retry Pattern**

Para testarmos as retentativa de acesso, vamos interromper a execução da App.B. Após isso vamos fazer uma chamada através da aplicação A. Nesse momento você deverá ver uma Exception informando que o serviço não foi encontrado.

Vamos configurar agora a aplicação A para que ela tente novamente o acesso ao outro serviço em determinado tempo, com o objetivo de aguardar que o serviço inacessivel se reestabeleça. Para isso inclua na classe Startup o seguinte trecho de código na nossa chamada http:

```csharp
public void ConfigureServices(IServiceCollection services)
{
    services.AddHttpClient(IService, Service)
        .AddTransientHttpErrorPolicy(
            policy => policy.WaitAndRetryAsync(3, _ => TimeSpan.FromMilliseconds(600))
        );
    // ...
}
```

Ao testar novamente a aplicação, você ira notar que a aplicação demorar vários segundos antes de estourar a exception novamente. Isso ocorre porque configuramos a aplicação para realizar uma nova tentativa a cada 600 milisegundos, em um total de 3 tentativas. Você pode acompanhar também através dos logs no terminal, onde você executou a aplicação. Observer que a partir da primeira chamada, após o tempo determinado ele exibe retentativa:

	<Imagem>
	
Você pode repetir o procedimento também na aplicação B, e simular a indisponibilidade da aplicação que lista as informações do tempo. Observe também que, ao executar novamente a aplicação que está indisponivel, a aplicação que realiza a chamada irá responder normalmente após o termino do tempo estipulado.

### **Circuit-Breaker**

O circuit-breaker com o Polly pode ser configurado juntamente com o retry. Adicione o código a seguir:

	public void ConfigureServices(IServiceCollection services)
	{
		services.AddHttpClient(IService, Service)
			.AddTransientHttpErrorPolicy(
				policy => policy.WaitAndRetryAsync(3, _ => TimeSpan.FromMilliseconds(600))
			)
			.AddTransientHttpErrorPolicy(
				policy => policy.CircuitBreakerAsync(5, TimeSpan.FromSeconds(30))
			);
		// ...
	}
	
Nesse cenário, vamos deixar a aplicação C indisponivel. Uma vez que a aplicação A, chama o serviço da aplicação B, o tempo de esperar seria muito maior caso o circuit breaker nao esteja configurado. Antes da configuração anterior faça o teste. Agora, o circuito será aberto e não permitirá outras requisições até que o tempo estimado passe, mais uma vez dando tempo para que a aplicação C se recupere.


Para obter maiores informações sobre os recursos da biblioteca, acesse o [github](https://github.com/App-vNext/Polly) do Polly. [Este]([https://github.com/App-vNext/Polly/wiki/Polly-and-HttpClientFactory]) trecho da documentação informa como trabalhar com o http client.

Cada linguagem de programação ou framework possui uma biblioteca que implementa esses padrões de resiliencia. Durante o desenvolvimento, se apronfundando no assunto e seguindo as ideias apresentadas nesse post, a resiliencia em suas aplicações poderão ser executadas com eficiencia. Mas existem outras abordagens que independem de linguagem de programação e consegue obter os mesmos e ate melhores resultados.


## **SERVICE MESH**

O Service Mesh é uma solução composta de ferramentas que permite gerenciar os diferentes componentes de uma aplicação e como eles se comunicam. Com o service mesh, ou malha de serviços, você consegue controlar diversos aspectos de sistemas distribuidos, como observabilidade, segurança, gerenciamento de trafego, este último promovendo o assunto desse post. Quando usamos o service mesh para tratar a resiliencia de nossas aplicações, não é necessário configurar nada diretamente em nossos códigos. A malha de serviços é adicionada a infraestrutura da aplicação, habilitando uma camada que consegue controlar as interações internas. Essa camada por sua vez que ficará responsável por realizar os retries, ou até mesmo controlando o circuit-breaker, sendo realizados automaticamente conforme configuração.

Para que isso seja possível, é necessário que o service mesh seja incorporado à aplicação como uma matriz de proxies, formando uma rede em malha. Em um service mesh, as solicitações são encaminhadas entre microsserviços utilizando os proxies dessa nova camada. Esses proxies são comumente chamados de "sidecar proxy", pois são executados em paralelo com cada serviço e não dentro dele. A titulo de curiosidade, um sidecar no mundo real é aquele carrinho de uma única roda que é acoplado a uma motocicleta para que se possa levar um outro passageiro.

Em cenários de produção utilizando o Kubernetes, a ferramenta mais conhecida e utilizada atualmente é o Istio. Existem outras opções de service mesh como o Linkerd, o Consul e o A seguir vamos ver algumas caracteristicas dessa solução em um exemplo prático, que irá tratar especificamente do controle de trafego.

## **Exemplo - Istio**

Antes de começar nossos testes é muito importante que você já tenha um conhecimento básico de Kubernetes. Como você deve saber, para executá-lo localmente você pode seguir uma das opções presentes [aqui](https://kubernetes.io/docs/tasks/tools/). Outras soluções como o [K3D](https://k3d.io/v5.0.3/#installation) também pode ser executada localmente para criar um cluster com vários nodes. Para facilitar ainda mais as coisas (para usuários Windows), podemos utilizar o Kubernetes pré-instalado com o [Docker Desktop](https://docs.docker.com/desktop/kubernetes/).

Instalar o Istio localmente também é relativamente fácil. Precisamos primeiramente baixar o último release disponível no [github](https://github.com/istio/istio/releases/tag/1.11.4) do Istio para o sistema operacional escolhido. Depois de extrair os arquivos em algum caminho ou pasta de preferência, você deve adicioná-lo na variavel de ambiente PATH do seu usuário. Ao digitar "istioctl" no terminal de comando você deverá ver as opções do Istio. Mais detalhes da instalação você pode acompanhar aqui[link get started].

O Istio possui vários perfis de configuração, cada com um com uma caracateristica diferente, dependendo da necessidade de utilização. Para nosso simples cenário, vamos utilizar o perfil padrão, que irá istalar tudo o que precisamos:

	istioctl install --set profile=demo
	
Como explicado anteriormente, precisamos incorporar o sidecar proxy nos nossos serviços. O comando abaixo irá habilitar a injeção do sidecar sempre que um novo Pod[link Kubernetes POD] for criado:

	kubectl label namespace default istio-injection=enabled
	
Agora, para verificar a instalação e se tudo está ok, basta executar os comandos abaixo:

	kubectl get namespace -L istio-injection

	NAME              STATUS   AGE    ISTIO-INJECTION
	default           Active   3m     enabled
	istio-system      Active   63s    disabled
	
	kubectl get svc -n istio-system
	
	NAME                   TYPE           CLUSTER-IP      EXTERNAL-IP
	istio-egressgateway    ClusterIP      10.55.252.182   <none>
	istio-ingressgateway   LoadBalancer   10.55.250.185   localhost
	istiod                 ClusterIP      10.55.253.217   <none>
	
	kubectl get pods -n istio-system
	
	NAME                                    READY   STATUS
	istio-egressgateway-674988f895-m6tk4    1/1     Running
	istio-ingressgateway-6996f7dcc8-7lvm2   1/1     Running
	istiod-6bf5fc8b64-j79hj                 1/1     Running

O obejtivo destes testes como o Istio é realizar os mesmos procedimentos executados com o Polly, mas agora sem nenhuma configuração em nosso código. Para isso preparei a mesma aplicação exemplo sem tais bibliotecas e estão disponiveis no meu [docker.hub](docker.hub.com). Os arquivos utilizados daqui pra frente também estarão disponíveis no meu [github](github.com) e irei explica-los na medida que avançamos.

Para realizar o deploy da aplicação, execute o comando a seguir no terminal:

	$ kubectl apply -f https://raw.githubusercontent.com/(...)/deployment.yaml
	
Ou se preferir utilize o template abaixo para fazer o deploy com suas proprias imagens, dando um apply no arquivo YAML criado:

```YAML
apiVersion: v1
kind: Service
metadata:
    name: aspnetcore-service
    labels:
    app: aspnetcore
spec:
    ports:
    - port: 8080
    name: http
    selector:
    app: aspnetcore
    
---

apiVersion: apps/v1
kind: Deployment
metadata:
    name: aspnetcore-v1
spec:
    replicas: 1
    selector:
    matchLabels:
        app: aspnetcore
        version: v1
    template:
    metadata:
        labels:
        app: aspnetcore
        version: v1
    spec:
        containers:
        - name: aspnetcore
        image: <NOME_DA_IMAGEM>
        imagePullPolicy: Never
        ports:
        - containerPort: 8080
```      
	
O arquivo acima irá criar os [Deployments](https://kubernetes.io/docs/concepts/workloads/controllers/deployment/) e os [Services](https://kubernetes.io/docs/concepts/services-networking/service/) para sua aplicação. Para configurar o acesso externo à nossa aplicação, devemos executar o comando abaixo para aplicarmos o [Gateway](https://istio.io/latest/docs/reference/config/networking/gateway/) e o [VirtualService](https://istio.io/latest/docs/reference/config/networking/virtual-service/):


```
$ kubectl apply -f https://raw.githubusercontent.com/(...)/gateway.yaml
```

O Gateway faz isso e isso e isso. Já o VirtualSerive faz isso e isso e isso. Caso esteja utilizando suas própias imagens, altere o arquivo abaixo conforme sua necessidade:

```Yaml
apiVersion: networking.istio.io/v1alpha3
kind: Gateway
metadata:
    name: aspnetcore-gateway
spec:
    selector:
    istio: ingressgateway # use istio default controller
    servers:
    - port:
        number: 80
        name: http
        protocol: HTTP
    hosts:
    - "*"

---

apiVersion: networking.istio.io/v1alpha3
kind: VirtualService
metadata:
    name: aspnetcore-virtualservice
spec:
    hosts:
    - "*"
    gateways:
    - aspnetcore-gateway
    http:
    - route:
    - destination:
        host: aspnetcore-service
```

Executando os comandos abaixo você poderá ver os seus Pods[link kuberntes] e o seus Sevices[link kubernetes] criados:

	$ kubectl get services
	
	*************

	$ kubectl get pods
	
	NAME                          READY     STATUS    RESTARTS   AGE
	aspnetcore-v1-6cf64748-mddb   2/2       Running   0          34s
	
Observe na colunda READY que temos dois containers dentro de cada POD: Um para a aplicação e o outro do sidecar.
	
Agora ao acessar o caminho http://localhost:80 você será redirecionado para página inicial da aplicação:

<imagem>

### **Retry Pattern**

Para testarmos as retentativa de acesso, vamos interromper a execução da App.B. Após isso vamos fazer uma chamada através da aplicação A. Nesse momento você deverá ver uma Exception informando que o serviço não foi encontrado.

Vamos configurar agora a aplicação A para que ela tente novamente o acesso ao outro serviço em determinado tempo, com o objetivo de aguardar que o serviço inacessivel se reestabeleça. Para isso inclua na classe Startup o seguinte trecho de código na nossa chamada http:

Ao testar novamente a aplicação, você ira notar que a aplicação demorar vários segundos antes de estourar a exception novamente. Isso ocorre porque configuramos a aplicação para realizar uma nova tentativa a cada 600 milisegundos, em um total de 3 tentativas. Você pode acompanhar também através dos logs no terminal, onde você executou a aplicação. Observer que a partir da primeira chamada, após o tempo determinado ele exibe retentativa:

	<Imagem>
	
Você pode repetir o procedimento também na aplicação B, e simular a indisponibilidade da aplicação que lista as informações do tempo. Observe também que, ao executar novamente a aplicação que está indisponivel, a aplicação que realiza a chamada irá responder normalmente após o termino do tempo estipulado.

### **Circuit-Breaker**

O circuit-breaker com o Polly pode ser configurado juntamente com o retry. Adicione o código a seguir:
	
Nesse cenário, vamos deixar a aplicação C indisponivel. Uma vez que a aplicação A, chama o serviço da aplicação B, o tempo de esperar seria muito maior caso o circuit breaker nao esteja configurado. Antes da configuração anterior faça o teste. Agora, o circuito será aberto e não permitirá outras requisições até que o tempo estimado passe, mais uma vez dando tempo para que a aplicação C se recupere.

## **Conclusão**




