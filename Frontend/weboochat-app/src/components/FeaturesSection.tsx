import { Bot, MessageSquare, Users, History } from "lucide-react";
import { Card, CardContent } from "@/components/ui/card";

const FeaturesSection = () => {
  const features = [
    {
      icon: MessageSquare,
      title: "WhatsApp Integration",
      description: "Connect directly with WhatsApp and centralize all conversations in a single intuitive and efficient platform.",
      color: "text-secondary"
    },
    {
      icon: Bot,
      title: "ChatGPT Automated Support",
      description: "Harness the power of artificial intelligence to automatically answer your customers' most common questions.",
      color: "text-primary"
    },
    {
      icon: Users,
      title: "Smart Routing",
      description: "Intelligent system that identifies when it's necessary to transfer the conversation to a human agent.",
      color: "text-accent"
    },
    {
      icon: History,
      title: "Complete History & Records",
      description: "Keep a detailed record of all conversations and interactions for better tracking and analysis.",
      color: "text-primary-glow"
    }
  ];

  return (
    <section id="features" className="py-20 bg-muted/30">
      <div className="container mx-auto px-4 lg:px-8">
        <div className="text-center mb-16">
          <h2 className="text-4xl md:text-5xl font-bold text-foreground mb-6">
            Features that
            <span className="bg-gradient-primary bg-clip-text text-transparent"> Transform</span>
          </h2>
          <p className="text-xl text-muted-foreground max-w-3xl mx-auto">
            Discover how our platform revolutionizes customer service with cutting-edge technology
          </p>
        </div>

        <div className="grid md:grid-cols-2 gap-8 max-w-5xl mx-auto">
          {features.map((feature, index) => (
            <Card 
              key={index} 
              className="group hover:shadow-card transition-all duration-300 hover:scale-105 bg-card/50 backdrop-blur-sm border-primary/10 hover:border-primary/30"
            >
              <CardContent className="p-8">
                <div className="flex items-start space-x-4">
                  <div className={`p-3 rounded-lg bg-gradient-secondary ${feature.color}`}>
                    <feature.icon size={32} />
                  </div>
                  <div className="flex-1">
                    <h3 className="text-xl font-semibold text-foreground mb-3 group-hover:text-primary transition-colors">
                      {feature.title}
                    </h3>
                    <p className="text-muted-foreground leading-relaxed">
                      {feature.description}
                    </p>
                  </div>
                </div>
              </CardContent>
            </Card>
          ))}
        </div>

        {/* Additional info */}
        <div className="mt-16 text-center">
          <div className="inline-flex items-center space-x-2 bg-gradient-secondary rounded-full px-6 py-3 border border-primary/20">
            <Bot className="w-5 h-5 text-primary" />
            <span className="text-sm font-medium text-foreground">
              Integration in under 5 minutes
            </span>
          </div>
        </div>
      </div>
    </section>
  );
};

export default FeaturesSection;